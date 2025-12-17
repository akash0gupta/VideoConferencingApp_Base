using LinqToDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.IAuthServices;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Application.Common.IEventHandlerServices;
using VideoConferencingApp.Application.DTOs.Common;
using VideoConferencingApp.Application.DTOs.Files;
using VideoConferencingApp.Domain.CacheKeys;
using VideoConferencingApp.Domain.Entities.FileEntities;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Exceptions;
using VideoConferencingApp.Domain.Interfaces;

namespace VideoConferencingApp.Application.Services.FileServices
{
    public class UserFileManagerService : IUserFileManagerService
    {
        private readonly IRepository<UserFile> _fileRepo;
        private readonly IRepository<TFileShare> _shareRepo;
        private readonly IRepository<TFileAccess> _accessRepo;
        private readonly IRepository<UserFolder> _folderRepo;
        private readonly IRepository<UserStorageQuota> _quotaRepo;
        private readonly IFileManagerService _fileManagerService;
        private readonly IStaticCacheManager _cache;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<UserFileManagerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IBCryptPasswordServices _authService;

        // Configuration
        private readonly long _defaultStorageLimit;
        private readonly string[] _allowedExtensions;

        public UserFileManagerService(
            IRepository<UserFile> fileRepo,
            IRepository<TFileShare> shareRepo,
            IRepository<TFileAccess> accessRepo,
            IRepository<UserFolder> folderRepo,
            IRepository<UserStorageQuota> quotaRepo,
            IFileManagerService fileManagerService,
            IStaticCacheManager cache,
            IEventPublisher eventPublisher,
            ILogger<UserFileManagerService> logger,
            IBCryptPasswordServices authService,
            IConfiguration configuration)
        {
            _fileRepo = fileRepo ?? throw new ArgumentNullException(nameof(fileRepo));
            _shareRepo = shareRepo ?? throw new ArgumentNullException(nameof(shareRepo));
            _accessRepo = accessRepo ?? throw new ArgumentNullException(nameof(accessRepo));
            _folderRepo = folderRepo ?? throw new ArgumentNullException(nameof(folderRepo));
            _quotaRepo = quotaRepo ?? throw new ArgumentNullException(nameof(quotaRepo));
            _fileManagerService = fileManagerService ?? throw new ArgumentNullException(nameof(fileManagerService));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            _defaultStorageLimit = _configuration.GetValue<long>("FileUpload:DefaultStorageLimit", 5L * 1024 * 1024 * 1024); // 5GB
            _allowedExtensions = _configuration.GetSection("FileUpload:AllowedExtensions").Get<string[]>()
                ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip" };
        }

        #region File Operations

        public async Task<FileDto> UploadFileAsync(long userId, FileUploadDto dto)
        {
            try
            {
                // Validate file
                var validationSettings = new FileUploadSettings
                {
                    MaxFileSize = _configuration.GetValue<long>("FileUpload:MaxFileSize", 104857600),
                    AllowedExtensions = _allowedExtensions,
                    ValidateMimeType = true
                };

                var validationResult = FileDocumentHelper.ValidateFile(dto.File, validationSettings);
                if (!validationResult.IsValid)
                    throw new ValidationException(validationResult.ErrorMessage);

                // Validate storage quota
                if (!await ValidateUserStorageAsync(userId, dto.File.Length))
                    throw new InvalidOperationException("Storage quota exceeded");

                // Calculate file hash for duplicate detection
                string fileHash;
                using (var stream = dto.File.OpenReadStream())
                {
                    fileHash = await FileDocumentHelper.CalculateFileHashAsync(stream);
                }

                // Check for duplicates
                var existingFile = await _fileRepo.FindAsync(f =>
                    f.UserId == userId &&
                    f.FileHash == fileHash &&
                    !f.IsDeleted);

                if (existingFile.Any())
                {
                    _logger.LogWarning("Duplicate file detected for user {UserId}: {Hash}", userId, fileHash);
                    throw new InvalidOperationException("This file already exists in your storage");
                }

                // Prepare folder path
                string folderPath = null;
                if (!string.IsNullOrEmpty(dto.FolderId))
                {
                    var folder = await _folderRepo.FindAsync(f =>
                        f.FolderId == dto.FolderId &&
                        f.UserId == userId &&
                        !f.IsDeleted);

                    if (folder.Any())
                        folderPath = folder.First().Path;
                }

                // Save file to storage
                string filePath;
                using (var stream = dto.File.OpenReadStream())
                {
                    filePath = await _fileManagerService.SaveFileAsync(
                        stream,
                        dto.File.FileName,
                        dto.File.ContentType,
                        $"users/{userId}/{folderPath}");
                }

                // Generate thumbnail for images
                string thumbnailPath = null;
                if (FileDocumentHelper.IsImage(Path.GetExtension(dto.File.FileName)))
                {
                    thumbnailPath = await _fileManagerService.GenerateThumbnailAsync(filePath, 200, 200);
                }

                // Create file entity
                var userFile = new UserFile
                {
                    UserId = userId,
                    FileName = Path.GetFileNameWithoutExtension(dto.File.FileName),
                    OriginalFileName = dto.File.FileName,
                    FileExtension = Path.GetExtension(dto.File.FileName),
                    ContentType = dto.File.ContentType,
                    FileSize = dto.File.Length,
                    FilePath = filePath,
                    FileHash = fileHash,
                    ThumbnailPath = thumbnailPath,
                    Visibility = dto.Visibility,
                    Description = dto.Description,
                    Tags = dto.Tags,
                    FolderId = dto.FolderId,
                    ExpiresAt = dto.ExpiresAt,
                    IsEncrypted = dto.Encrypt,
                    DownloadCount = 0,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false
                };

                await _fileRepo.InsertAsync(userFile);

                // Update user storage quota
                await UpdateUserStorageQuotaAsync(userId, dto.File.Length, true);

                // Log access
                await LogFileAccessAsync(userFile.Id, userId, FileAccessType.Upload);

                // Clear cache
                await ClearUserFileCacheAsync(userId);

                //// Publish event
                //await _eventPublisher.PublishAsync(new FileUploadedEvent
                //{
                //    FileId = userFile.Id,
                //    UserId = userId,
                //    FileName = userFile.OriginalFileName,
                //    FileSize = userFile.FileSize,
                //    UploadedAt = userFile.CreatedOnUtc
                //});

                _logger.LogInformation("File uploaded successfully: FileId={FileId}, UserId={UserId}, FileName={FileName}",
                    userFile.Id, userId, userFile.OriginalFileName);

                return await MapToDto(userFile, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(long fileId, long userId)
        {
            try
            {
                var file = await _fileRepo.GetByIdAsync(fileId);
                if (file == null || file.IsDeleted)
                    throw new NotFoundException($"File with ID {fileId} not found");

                // Check permissions
                if (!await CanAccessFileAsync(file, userId))
                    throw new UnauthorizedException("You don't have permission to download this file");

                // Check if file has expired
                if (file.ExpiresAt.HasValue && file.ExpiresAt.Value < DateTime.UtcNow)
                    throw new InvalidOperationException("This file has expired");

                // Update download count and last accessed
                file.DownloadCount++;
                file.LastAccessedAt = DateTime.UtcNow;
                await _fileRepo.UpdateAsync(file);

                // Log access
                await LogFileAccessAsync(fileId, userId, FileAccessType.Download);

                // Get file stream from storage
                var stream = await _fileManagerService.GetFileAsync(file.FilePath);

                _logger.LogInformation("File downloaded: FileId={FileId}, UserId={UserId}", fileId, userId);

                return stream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file {FileId} for user {UserId}", fileId, userId);
                throw;
            }
        }

        public async Task<FileDto> GetFileAsync(long fileId, long userId)
        {
            try
            {
                var file = await _fileRepo.GetByIdAsync(fileId);
                if (file == null || file.IsDeleted)
                    throw new NotFoundException($"File with ID {fileId} not found");

                // Check permissions
                if (!await CanAccessFileAsync(file, userId))
                    throw new UnauthorizedException("You don't have permission to view this file");

                // Log access
                await LogFileAccessAsync(fileId, userId, FileAccessType.View);

                return await MapToDto(file, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file {FileId} for user {UserId}", fileId, userId);
                throw;
            }
        }

        public async Task<IPagedList<FileDto>> GetUserFilesAsync(long userId, FileSearchDto searchDto)
        {
            try
            {
                var cacheKey = UserFileCacheKey.UserFilesSearch(userId, searchDto.GetHashCode());

                return await _cache.GetOrCreateAsync<IPagedList<FileDto>>(cacheKey, async (ct) =>
                {
                    var query = await Task.FromResult(_fileRepo.Table.Where(f => f.UserId == userId && !f.IsDeleted));

                    // Apply search filter
                    if (!string.IsNullOrWhiteSpace(searchDto.Query))
                    {
                        var searchQuery = searchDto.Query.ToLower();
                        query = query.Where(f =>
                            f.FileName.ToLower().Contains(searchQuery) ||
                            (f.Description != null && f.Description.ToLower().Contains(searchQuery)) ||
                            (f.Tags != null && f.Tags.ToLower().Contains(searchQuery)));
                    }

                    // Apply visibility filter
                    if (searchDto.Visibility.HasValue)
                        query = query.Where(f => f.Visibility == searchDto.Visibility.Value);

                    // Apply extension filter
                    if (!string.IsNullOrWhiteSpace(searchDto.FileExtension))
                        query = query.Where(f => f.FileExtension.Equals(searchDto.FileExtension));

                    // Apply folder filter
                    if (!string.IsNullOrWhiteSpace(searchDto.FolderId))
                        query = query.Where(f => f.FolderId == searchDto.FolderId);

                    // Apply date range filter
                    if (searchDto.FromDate.HasValue)
                        query = query.Where(f => f.CreatedOnUtc >= searchDto.FromDate.Value);

                    if (searchDto.ToDate.HasValue)
                        query = query.Where(f => f.CreatedOnUtc <= searchDto.ToDate.Value);

                    // Apply size filter
                    if (searchDto.MinSize.HasValue)
                        query = query.Where(f => f.FileSize >= searchDto.MinSize.Value);

                    if (searchDto.MaxSize.HasValue)
                        query = query.Where(f => f.FileSize <= searchDto.MaxSize.Value);

                    var totalcount = query.Count();
                    var list = await query.ToListAsync();
                    // Apply sorting
                    list = ApplySorting(list, searchDto.OrderBy, searchDto.OrderDescending);

                    // Convert to DTOs
                    var fileDtos = new List<FileDto>();
                    foreach (var file in list)
                    {
                        fileDtos.Add(await MapToDto(file, userId));
                    }

                    return new PagedList<FileDto>(fileDtos, searchDto.PageNumber, searchDto.PageSize, totalcount);
                }, ttl: _cache.GetDefaultTtl());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files for user {UserId}", userId);
                throw;
            }
        }

        public async Task<FileDto> UpdateFileAsync(long fileId, long userId, UpdateFileDto dto)
        {
            try
            {
                var file = await _fileRepo.GetByIdAsync(fileId);
                if (file == null || file.IsDeleted)
                    throw new NotFoundException($"File with ID {fileId} not found");

                if (file.UserId != userId)
                    throw new UnauthorizedException("You can only update your own files");

                // Update file properties
                if (!string.IsNullOrWhiteSpace(dto.FileName))
                {
                    file.FileName = FileDocumentHelper.SanitizeFileName(dto.FileName);
                }

                if (!string.IsNullOrWhiteSpace(dto.Description))
                    file.Description = dto.Description;

                if (dto.Visibility.HasValue)
                    file.Visibility = dto.Visibility.Value;

                if (dto.Tags != null)
                    file.Tags = dto.Tags;

                if (!string.IsNullOrWhiteSpace(dto.FolderId))
                {
                    // Validate folder exists
                    var folder = await _folderRepo.FindAsync(f =>
                        f.FolderId == dto.FolderId &&
                        f.UserId == userId &&
                        !f.IsDeleted);

                    if (folder.Any())
                        file.FolderId = dto.FolderId;
                }

                file.UpdatedOnUtc = DateTime.UtcNow;
                await _fileRepo.UpdateAsync(file);

                // Clear cache
                await ClearUserFileCacheAsync(userId);

                // Log activity
                await LogFileAccessAsync(fileId, userId, FileAccessType.Update);

                _logger.LogInformation("File updated: FileId={FileId}, UserId={UserId}", fileId, userId);

                return await MapToDto(file, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating file {FileId} for user {UserId}", fileId, userId);
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(long fileId, long userId)
        {
            try
            {
                var file = await _fileRepo.GetByIdAsync(fileId);
                if (file == null || file.IsDeleted)
                    throw new NotFoundException($"File with ID {fileId} not found");

                if (file.UserId != userId)
                    throw new UnauthorizedException("You can only delete your own files");

                // Delete file from storage
                await _fileManagerService.DeleteFileAsync(file.FilePath);

                // Delete thumbnail if exists
                if (!string.IsNullOrEmpty(file.ThumbnailPath))
                    await _fileManagerService.DeleteFileAsync(file.ThumbnailPath);

                // Soft delete the database record
                file.IsDeleted = true;
                file.UpdatedOnUtc = DateTime.UtcNow;
                await _fileRepo.UpdateAsync(file);

                // Update user storage quota
                await UpdateUserStorageQuotaAsync(userId, -file.FileSize, false);

                // Clear cache
                await ClearUserFileCacheAsync(userId);

                // Log activity
                await LogFileAccessAsync(fileId, userId, FileAccessType.Delete);

                //// Publish event
                //await _eventPublisher.PublishAsync(new FileDeletedEvent
                //{
                //    FileId = fileId,
                //    UserId = userId,
                //    FileName = file.OriginalFileName,
                //    DeletedAt = DateTime.UtcNow
                //});

                _logger.LogInformation("File deleted: FileId={FileId}, UserId={UserId}", fileId, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileId} for user {UserId}", fileId, userId);
                throw;
            }
        }

        public async Task<bool> DeleteFilesAsync(List<long> fileIds, long userId)
        {
            var allDeleted = true;

            foreach (var fileId in fileIds)
            {
                try
                {
                    await DeleteFileAsync(fileId, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting file {FileId} in batch for user {UserId}", fileId, userId);
                    allDeleted = false;
                }
            }

            return allDeleted;
        }

        #endregion

        #region Folder Operations

        public async Task<FolderDto> CreateFolderAsync(long userId, CreateFolderDto dto)
        {
            try
            {
                // Sanitize folder name
                var folderName = FileDocumentHelper.SanitizeFileName(dto.FolderName);

                // Generate unique folder ID
                var folderId = Guid.NewGuid().ToString("N");

                // Build folder path
                string path;
                if (!string.IsNullOrEmpty(dto.ParentFolderId))
                {
                    var parentFolder = await _folderRepo.FindAsync(f =>
                        f.FolderId == dto.ParentFolderId &&
                        f.UserId == userId &&
                        !f.IsDeleted);

                    if (!parentFolder.Any())
                        throw new NotFoundException("Parent folder not found");

                    path = $"{parentFolder.First().Path}/{folderName}";
                }
                else
                {
                    path = folderName;
                }

                // Check if folder already exists
                var existingFolder = await _folderRepo.FindAsync(f =>
                    f.Path == path &&
                    f.UserId == userId &&
                    !f.IsDeleted);

                if (existingFolder.Any())
                    throw new InvalidOperationException("Folder already exists");

                // Create folder entity
                var folder = new UserFolder
                {
                    UserId = userId,
                    FolderId = folderId,
                    FolderName = folderName,
                    ParentFolderId = dto.ParentFolderId,
                    Path = path,
                    Visibility = dto.Visibility,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false
                };

                await _folderRepo.InsertAsync(folder);

                // Clear cache
                await ClearUserFolderCacheAsync(userId);

                _logger.LogInformation("Folder created: FolderId={FolderId}, UserId={UserId}, Name={FolderName}",
                    folderId, userId, folderName);

                return MapFolderToDto(folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<FolderDto>> GetUserFoldersAsync(long userId)
        {
            try
            {
                var cacheKey = UserFileCacheKey.UserFoldersByUserId(userId);

                return await _cache.GetOrCreateAsync(cacheKey, async (ct) =>
                {
                    var folders = await _folderRepo.FindAsync(f => f.UserId == userId && !f.IsDeleted);

                    var folderDtos = folders.Select(f => MapFolderToDto(f)).ToList();

                    // Build folder hierarchy
                    var rootFolders = folderDtos.Where(f => string.IsNullOrEmpty(f.ParentFolderId)).ToList();
                    foreach (var rootFolder in rootFolders)
                    {
                        BuildFolderHierarchy(rootFolder, folderDtos);
                    }

                    return rootFolders;
                }, TimeSpan.FromMinutes(10));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folders for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteFolderAsync(string folderId, long userId)
        {
            try
            {
                var folder = await _folderRepo.FindAsync(f =>
                    f.FolderId == folderId &&
                    f.UserId == userId &&
                    !f.IsDeleted);

                if (!folder.Any())
                    throw new NotFoundException("Folder not found");

                var folderToDelete = folder.First();

                // Check if folder has files
                var filesInFolder = await _fileRepo.FindAsync(f =>
                    f.FolderId == folderId &&
                    f.UserId == userId &&
                    !f.IsDeleted);

                if (filesInFolder.Any())
                    throw new InvalidOperationException("Cannot delete folder containing files");

                // Check for subfolders
                var subFolders = await _folderRepo.FindAsync(f =>
                    f.ParentFolderId == folderId &&
                    f.UserId == userId &&
                    !f.IsDeleted);

                if (subFolders.Any())
                    throw new InvalidOperationException("Cannot delete folder containing subfolders");

                // Soft delete folder
                folderToDelete.IsDeleted = true;
                folderToDelete.UpdatedOnUtc = DateTime.UtcNow;
                await _folderRepo.UpdateAsync(folderToDelete);

                // Clear cache
                await ClearUserFolderCacheAsync(userId);

                _logger.LogInformation("Folder deleted: FolderId={FolderId}, UserId={UserId}", folderId, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder {FolderId} for user {UserId}", folderId, userId);
                throw;
            }
        }

        public async Task<FileDto> MoveFileAsync(long userId, MoveFileDto dto)
        {
            try
            {
                var file = await _fileRepo.GetByIdAsync(dto.FileId);
                if (file == null || file.IsDeleted)
                    throw new NotFoundException($"File with ID {dto.FileId} not found");

                if (file.UserId != userId)
                    throw new UnauthorizedException("You can only move your own files");

                // Validate target folder if provided
                if (!string.IsNullOrEmpty(dto.TargetFolderId))
                {
                    var targetFolder = await _folderRepo.FindAsync(f =>
                        f.FolderId == dto.TargetFolderId &&
                        f.UserId == userId &&
                        !f.IsDeleted);

                    if (!targetFolder.Any())
                        throw new NotFoundException("Target folder not found");
                }

                // Update file folder
                file.FolderId = dto.TargetFolderId;
                file.UpdatedOnUtc = DateTime.UtcNow;
                await _fileRepo.UpdateAsync(file);

                // Clear cache
                await ClearUserFileCacheAsync(userId);

                _logger.LogInformation("File moved: FileId={FileId}, TargetFolder={FolderId}, UserId={UserId}",
                    dto.FileId, dto.TargetFolderId, userId);

                return await MapToDto(file, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving file {FileId} for user {UserId}", dto?.FileId, userId);
                throw;
            }
        }

        #endregion

        #region Sharing Operations

        public async Task<FileShareDto> ShareFileAsync(long userId, CreateFileShareDto dto)
        {
            try
            {
                var file = await _fileRepo.GetByIdAsync(dto.FileId);
                if (file == null || file.IsDeleted)
                    throw new NotFoundException($"File with ID {dto.FileId} not found");

                if (file.UserId != userId)
                    throw new UnauthorizedException("You can only share your own files");

                // Generate unique share token
                var shareToken = GenerateShareToken();

                // Hash password if provided
                string hashedPassword = null;
                if (!string.IsNullOrEmpty(dto.Password))
                {
                    hashedPassword = _authService.HashPassword(dto.Password);
                }

                // Create share entity
                var share = new TFileShare
                {
                    FileId = dto.FileId,
                    SharedById = userId,
                    SharedWithUserId = dto.SharedWithUserId,
                    ShareToken = shareToken,
                    Permission = dto.Permission,
                    ExpiresAt = dto.ExpiresAt,
                    Password = hashedPassword,
                    MaxAccessCount = dto.MaxAccessCount,
                    AccessCount = 0,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow,
                    IsActive = true
                };

                await _shareRepo.InsertAsync(share);

                // Log activity
                await LogFileAccessAsync(dto.FileId, userId, FileAccessType.Share);

                //// Publish event
                //await _eventPublisher.PublishAsync(new FileSharedEvent
                //{
                //    FileId = dto.FileId,
                //    ShareId = share.Id,
                //    SharedById = userId,
                //    SharedWithUserId = dto.SharedWithUserId,
                //    ShareToken = shareToken,
                //    SharedAt = share.CreatedOnUtc
                //});

                _logger.LogInformation("File shared: FileId={FileId}, ShareId={ShareId}, UserId={UserId}",
                    dto.FileId, share.Id, userId);

                return MapShareToDto(share, file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing file {FileId} by user {UserId}", dto?.FileId, userId);
                throw;
            }
        }

        public async Task<List<FileShareDto>> GetFileSharesAsync(long fileId, long userId)
        {
            try
            {
                var file = await _fileRepo.GetByIdAsync(fileId);
                if (file == null || file.IsDeleted)
                    throw new NotFoundException($"File with ID {fileId} not found");

                if (file.UserId != userId)
                    throw new UnauthorizedException("You can only view shares for your own files");

                var shares = await _shareRepo.FindAsync(s => s.FileId == fileId && s.IsActive);

                return shares.Select(s => MapShareToDto(s, file)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shares for file {FileId} by user {UserId}", fileId, userId);
                throw;
            }
        }

        public async Task<bool> RevokeShareAsync(long shareId, long userId)
        {
            try
            {
                var share = await _shareRepo.GetByIdAsync(shareId);
                if (share == null)
                    throw new NotFoundException($"Share with ID {shareId} not found");

                if (share.SharedById != userId)
                    throw new UnauthorizedException("You can only revoke your own shares");

                // Deactivate share
                share.IsActive = false;
                share.UpdatedOnUtc = DateTime.UtcNow;
                await _shareRepo.UpdateAsync(share);

                _logger.LogInformation("Share revoked: ShareId={ShareId}, UserId={UserId}", shareId, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking share {ShareId} by user {UserId}", shareId, userId);
                throw;
            }
        }

        public async Task<Stream> DownloadSharedFileAsync(string shareToken, string password = null)
        {
            try
            {
                var share = await _shareRepo.FindAsync(s => s.ShareToken == shareToken && s.IsActive);
                if (!share.Any())
                    throw new NotFoundException("Share not found or expired");

                var shareRecord = share.First();

                // Check expiration
                if (shareRecord.ExpiresAt.HasValue && shareRecord.ExpiresAt.Value < DateTime.UtcNow)
                    throw new InvalidOperationException("This share has expired");

                // Check access count
                if (shareRecord.MaxAccessCount.HasValue && shareRecord.AccessCount >= shareRecord.MaxAccessCount.Value)
                    throw new InvalidOperationException("Maximum access count reached for this share");

                // Verify password if required
                if (!string.IsNullOrEmpty(shareRecord.Password))
                {
                    if (string.IsNullOrEmpty(password) || _authService.VerifyPassword(password, shareRecord.Password))
                        throw new UnauthorizedException("Invalid password");
                }

                // Get file
                var file = await _fileRepo.GetByIdAsync(shareRecord.FileId);
                if (file == null || file.IsDeleted)
                    throw new NotFoundException("File not found");

                // Check permission
                if (shareRecord.Permission < FilePermission.Download)
                    throw new UnauthorizedException("This share does not allow downloads");

                // Update access count
                shareRecord.AccessCount++;
                shareRecord.UpdatedOnUtc = DateTime.UtcNow;
                await _shareRepo.UpdateAsync(shareRecord);

                // Log access
                await LogFileAccessAsync(file.Id, shareRecord.SharedWithUserId, FileAccessType.Download, shareToken);

                // Get file stream
                return await _fileManagerService.GetFileAsync(file.FilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading shared file with token {ShareToken}", shareToken);
                throw;
            }
        }

        public async Task<FileDto> GetSharedFileInfoAsync(string shareToken, string password = null)
        {
            try
            {
                var share = await _shareRepo.FindAsync(s => s.ShareToken == shareToken && s.IsActive);
                if (!share.Any())
                    throw new NotFoundException("Share not found or expired");

                var shareRecord = share.First();

                // Check expiration
                if (shareRecord.ExpiresAt.HasValue && shareRecord.ExpiresAt.Value < DateTime.UtcNow)
                    throw new InvalidOperationException("This share has expired");

                // Verify password if required
                if (!string.IsNullOrEmpty(shareRecord.Password))
                {
                    if (string.IsNullOrEmpty(password) || _authService.VerifyPassword(password, shareRecord.Password))
                        throw new UnauthorizedException("Invalid password");
                }

                // Get file
                var file = await _fileRepo.GetByIdAsync(shareRecord.FileId);
                if (file == null || file.IsDeleted)
                    throw new NotFoundException("File not found");

                return await MapToDto(file, shareRecord.SharedWithUserId ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shared file info with token {ShareToken}", shareToken);
                throw;
            }
        }

        #endregion

        #region Statistics and Access Logs

        public async Task<List<FileAccessDto>> GetFileAccessLogsAsync(long fileId, long userId)
        {
            try
            {
                var file = await _fileRepo.GetByIdAsync(fileId);
                if (file == null || file.IsDeleted)
                    throw new NotFoundException($"File with ID {fileId} not found");

                if (file.UserId != userId)
                    throw new UnauthorizedException("You can only view access logs for your own files");

                var logs = await _accessRepo.FindAsync(a => a.FileId == fileId);

                return logs.OrderByDescending(l => l.AccessedAt)
                    .Select(l => new FileAccessDto
                    {
                        Id = l.Id,
                        FileId = l.FileId,
                        FileName = file.OriginalFileName,
                        UserId = l.UserId,
                        AccessType = l.AccessType,
                        IpAddress = l.IpAddress,
                        AccessedAt = l.AccessedAt
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting access logs for file {FileId} by user {UserId}", fileId, userId);
                throw;
            }
        }

        public async Task<FileStatisticsDto> GetUserStatisticsAsync(long userId)
        {
            try
            {
                var cacheKey = UserFileCacheKey.UserStatisticsByUserId(userId);

                return await _cache.GetOrCreateAsync(cacheKey, async (ct) =>
                {
                    var files = await _fileRepo.FindAsync(f => f.UserId == userId && !f.IsDeleted);

                    var stats = new FileStatisticsDto
                    {
                        TotalFiles = files.Count,
                        TotalSize = files.Sum(f => f.FileSize),
                        FormattedTotalSize = FileDocumentHelper.FormatFileSize(files.Sum(f => f.FileSize)),
                        StorageLimit = _defaultStorageLimit,
                        FormattedStorageLimit = FileDocumentHelper.FormatFileSize(_defaultStorageLimit),
                        UsedStorage = files.Sum(f => f.FileSize),
                        FormattedUsedStorage = FileDocumentHelper.FormatFileSize(files.Sum(f => f.FileSize)),
                        StorageUsagePercentage = (double)files.Sum(f => f.FileSize) / _defaultStorageLimit * 100,
                        TotalDownloads = files.Sum(f => f.DownloadCount),
                        FilesByType = files.GroupBy(f => FileDocumentHelper.GetFileCategory(f.FileExtension))
                            .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                        FilesByVisibility = files.GroupBy(f => f.Visibility)
                            .ToDictionary(g => g.Key, g => g.Count()),
                        SharedFiles = await _shareRepo.CountAsync(s => s.SharedById == userId && s.IsActive)
                    };

                    return stats;
                }, TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ValidateUserStorageAsync(long userId, long fileSize)
        {
            try
            {
                var quota = await GetOrCreateUserQuotaAsync(userId);
                return (quota.UsedStorage + fileSize) <= quota.StorageLimit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating storage for user {UserId}", userId);
                return false;
            }
        }

        #endregion

        #region Bulk Operations

        public async Task<bool> BulkOperationAsync(long userId, BulkFileOperationDto dto)
        {
            try
            {
                switch (dto.Operation)
                {
                    case BulkOperation.Delete:
                        return await DeleteFilesAsync(dto.FileIds, userId);

                    case BulkOperation.Move:
                        if (string.IsNullOrEmpty(dto.TargetFolderId))
                            throw new ValidationException("Target folder is required for move operation");

                        foreach (var fileId in dto.FileIds)
                        {
                            await MoveFileAsync(userId, new MoveFileDto
                            {
                                FileId = fileId,
                                TargetFolderId = dto.TargetFolderId
                            });
                        }
                        return true;

                    case BulkOperation.ChangeVisibility:
                        if (!dto.Visibility.HasValue)
                            throw new ValidationException("Visibility is required for this operation");

                        foreach (var fileId in dto.FileIds)
                        {
                            await UpdateFileAsync(fileId, userId, new UpdateFileDto
                            {
                                Visibility = dto.Visibility
                            });
                        }
                        return true;

                    case BulkOperation.Download:
                        // This would typically create a zip file
                        // Implementation would depend on your requirements
                        throw new NotImplementedException("Bulk download not yet implemented");

                    case BulkOperation.Share:
                        // Bulk sharing logic
                        throw new NotImplementedException("Bulk share not yet implemented");

                    default:
                        throw new InvalidOperationException($"Unknown operation: {dto.Operation}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk operation {Operation} for user {UserId}",
                    dto?.Operation, userId);
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task<bool> CanAccessFileAsync(UserFile file, long userId)
        {
            // Owner always has access
            if (file.UserId == userId)
                return true;

            // Check visibility
            switch (file.Visibility)
            {
                case FileVisibility.Public:
                    return true;

                case FileVisibility.ContactsOnly:
                    // This would check if users are contacts
                    // Implementation depends on your contact system
                    return false;

                case FileVisibility.Private:
                    // Check if explicitly shared
                    var shares = await _shareRepo.FindAsync(s =>
                        s.FileId == file.Id &&
                        s.SharedWithUserId == userId &&
                        s.IsActive &&
                        (!s.ExpiresAt.HasValue || s.ExpiresAt > DateTime.UtcNow));
                    return shares.Any();

                default:
                    return false;
            }
        }

        private async Task LogFileAccessAsync(long fileId, long? userId, FileAccessType accessType, string accessToken = null)
        {
            try
            {
                var access = new TFileAccess
                {
                    FileId = fileId,
                    UserId = userId,
                    AccessToken = accessToken,
                    AccessType = accessType,
                    AccessedAt = DateTime.UtcNow,
                    CreatedOnUtc = DateTime.UtcNow,
                    IsActive = true
                };

                await _accessRepo.InsertAsync(access);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log file access for FileId={FileId}", fileId);
            }
        }

        private async Task<FileDto> MapToDto(UserFile file, long userId)
        {
            var dto = new FileDto
            {
                Id = file.Id,
                UserId = file.UserId,
                FileName = file.FileName,
                OriginalFileName = file.OriginalFileName,
                FileExtension = file.FileExtension,
                ContentType = file.ContentType,
                FileSize = file.FileSize,
                FormattedFileSize = FileDocumentHelper.FormatFileSize(file.FileSize),
                ThumbnailUrl = !string.IsNullOrEmpty(file.ThumbnailPath)
                    ? await _fileManagerService.GetFileUrlAsync(file.ThumbnailPath)
                    : null,
                Visibility = file.Visibility,
                Description = file.Description,
                Tags = string.IsNullOrEmpty(file.Tags)
                    ? new List<string>()
                    : file.Tags.Split(',').Select(t => t.Trim()).ToList(),
                FolderId = file.FolderId,
                DownloadCount = file.DownloadCount,
                CreatedAt = file.CreatedOnUtc,
                LastAccessedAt = file.LastAccessedAt,
                ExpiresAt = file.ExpiresAt,
                IsEncrypted = file.IsEncrypted,
                CanDownload = file.UserId == userId || file.Visibility == FileVisibility.Public,
                CanEdit = file.UserId == userId,
                CanDelete = file.UserId == userId,
                CanShare = file.UserId == userId,
                DownloadUrl = await _fileManagerService.GetFileUrlAsync(file.FilePath)
            };

            // Get folder path if exists
            if (!string.IsNullOrEmpty(file.FolderId))
            {
                var folder = await _folderRepo.FindAsync(f => f.FolderId == file.FolderId && !f.IsDeleted);
                if (folder.Any())
                    dto.FolderPath = folder.First().Path;
            }

            return dto;
        }

        private FolderDto MapFolderToDto(UserFolder folder)
        {
            return new FolderDto
            {
                FolderId = folder.FolderId,
                FolderName = folder.FolderName,
                ParentFolderId = folder.ParentFolderId,
                Path = folder.Path,
                Visibility = folder.Visibility,
                CreatedAt = folder.CreatedOnUtc,
                SubFolders = new List<FolderDto>()
            };
        }

        private FileShareDto MapShareToDto(TFileShare share, UserFile file)
        {
            return new FileShareDto
            {
                Id = share.Id,
                FileId = share.FileId,
                FileName = file.OriginalFileName,
                SharedById = share.SharedById,
                SharedWithUserId = share.SharedWithUserId,
                ShareToken = share.ShareToken,
                ShareUrl = $"/api/files/shared/{share.ShareToken}",
                Permission = share.Permission,
                CreatedAt = share.CreatedOnUtc,
                ExpiresAt = share.ExpiresAt,
                IsPasswordProtected = !string.IsNullOrEmpty(share.Password),
                AccessCount = share.AccessCount,
                MaxAccessCount = share.MaxAccessCount
            };
        }

        private void BuildFolderHierarchy(FolderDto parentFolder, List<FolderDto> allFolders)
        {
            var subFolders = allFolders.Where(f => f.ParentFolderId == parentFolder.FolderId).ToList();
            parentFolder.SubFolders = subFolders;

            foreach (var subFolder in subFolders)
            {
                BuildFolderHierarchy(subFolder, allFolders);
            }
        }

        private List<UserFile> ApplySorting(IList<UserFile> query, string orderBy, bool descending)
        {
            return orderBy?.ToLower() switch
            {
                "filename" => descending
                    ? query.OrderByDescending(f => f.FileName).ToList()
                    : query.OrderBy(f => f.FileName).ToList(),
                "filesize" => descending
                    ? query.OrderByDescending(f => f.FileSize).ToList()
                    : query.OrderBy(f => f.FileSize).ToList(),
                "downloadcount" => descending
                    ? query.OrderByDescending(f => f.DownloadCount).ToList()
                    : query.OrderBy(f => f.DownloadCount).ToList(),
                "lastaccessedat" => descending
                    ? query.OrderByDescending(f => f.LastAccessedAt).ToList()
                    : query.OrderBy(f => f.LastAccessedAt).ToList(),
                _ => descending
                    ? query.OrderByDescending(f => f.CreatedOnUtc).ToList()
                    : query.OrderBy(f => f.CreatedOnUtc).ToList()
            };
        }

        private async Task<UserStorageQuota> GetOrCreateUserQuotaAsync(long userId)
        {
            var quota = await _quotaRepo.FindAsync(q => q.UserId == userId);

            if (!quota.Any())
            {
                var newQuota = new UserStorageQuota
                {
                    UserId = userId,
                    StorageLimit = _defaultStorageLimit,
                    UsedStorage = 0,
                    LastCalculatedAt = DateTime.UtcNow,
                    CreatedOnUtc = DateTime.UtcNow,
                    UpdatedOnUtc = DateTime.UtcNow,
                    IsActive = true
                };

                await _quotaRepo.InsertAsync(newQuota);
                return newQuota;
            }

            return quota.First();
        }

        private async Task UpdateUserStorageQuotaAsync(long userId, long sizeChange, bool isAddition)
        {
            try
            {
                var quota = await GetOrCreateUserQuotaAsync(userId);

                if (isAddition)
                    quota.UsedStorage += Math.Abs(sizeChange);
                else
                    quota.UsedStorage = Math.Max(0, quota.UsedStorage - Math.Abs(sizeChange));

                quota.LastCalculatedAt = DateTime.UtcNow;
                quota.UpdatedOnUtc = DateTime.UtcNow;

                await _quotaRepo.UpdateAsync(quota);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update storage quota for user {UserId}", userId);
            }
        }

        private async Task ClearUserFileCacheAsync(long userId)
        {
            await _cache.RemoveByPrefixAsync($"user_files_{userId}");
        }

        private async Task ClearUserFolderCacheAsync(long userId)
        {
            await _cache.RemoveByPrefixAsync($"user_folders_{userId}");
        }

        private string GenerateShareToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "")
                .Substring(0, 22);
        }

        #endregion
    }
}
