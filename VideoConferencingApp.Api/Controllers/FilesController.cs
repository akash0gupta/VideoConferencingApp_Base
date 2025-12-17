using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VideoConferencingApp.Api.DTOs;
using VideoConferencingApp.API.Controllers.Base;
using VideoConferencingApp.Application.DTOs.Authentication;
using VideoConferencingApp.Application.DTOs.Files;
using VideoConferencingApp.Application.Services.FileServices;
using VideoConferencingApp.Domain.Exceptions;
using VideoConferencingApp.Domain.Interfaces;
using VideoConferencingApp.Infrastructure.Services.AuthServices;

namespace VideoConferencingApp.Api.Controllers
{

    public class FilesController : BaseController
    {
        private readonly IUserFileManagerService _fileService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public FilesController(
            IUserFileManagerService fileService,
            IMapper mapper,
            IConfiguration configuration,
            ICurrentUserService currentUserService,
            IHttpContextService httpContextService,
            IResponseHeaderService responseHeaderService,
            ILogger<FilesController> logger)
            : base(logger, currentUserService, httpContextService, responseHeaderService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        #region File Operations

        /// <summary>
        /// Upload a new file
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(104857600)] // 100MB limit
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
        [ProducesResponseType(typeof(ApiResponse<FileDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<FileDto>>> UploadFile([FromForm] FileUploadDto request)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<FileDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (request?.File == null || request.File.Length == 0)
                {
                    return Failure<FileDto>(
                        null,
                        "No file provided or file is empty.",
                        StatusCodes.Status400BadRequest);
                }

                // Validate file size
                //var maxFileSize = _configuration.GetValue<long>("FileStorage:MaxFileSizeInMB", 104857600);
                //if (request.File.Length > maxFileSize)
                //{
                //    return Failure<FileDto>(
                //        null,
                //        $"File size exceeds the maximum allowed size of {FileDocumentHelper.FormatFileSize(maxFileSize)}.",
                //        StatusCodes.Status413PayloadTooLarge);
                //}

                // Validate file extension
                var allowedExtensions = _configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>()
                    ?? new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip" };

                var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                if (!FileDocumentHelper.IsAllowedExtension(fileExtension, allowedExtensions))
                {
                    return Failure<FileDto>(
                        null,
                        $"File type '{fileExtension}' is not allowed.",
                        StatusCodes.Status400BadRequest);
                }

                // Upload file
                var result = await _fileService.UploadFileAsync(CurrentUserId.Value, request);

                _logger.LogInformation(
                    "File uploaded - FileId: {FileId} | FileName: {FileName} | Size: {Size} | UserId: {UserId} | TraceId: {TraceId}",
                    result.Id, result.FileName, result.FileSize, CurrentUserId, TraceId);

                // Set location header for created resource
                _responseHeaderService.SetCustomHeader("Location", $"/api/files/getfile?id={result.Id}");

                return StatusCode(StatusCodes.Status201Created, new ApiResponse<FileDto>
                {
                    Success = true,
                    Message = "File uploaded successfully.",
                    Data = result,
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (ValidationException ex)
            {
                return Failure<FileDto>(null, ex.Message, StatusCodes.Status400BadRequest);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Storage quota exceeded"))
            {
                return Failure<FileDto>(null, ex.Message, StatusCodes.Status413PayloadTooLarge);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Failure<FileDto>(null, ex.Message, StatusCodes.Status409Conflict);
            }
            catch (InvalidOperationException ex)
            {
                return Failure<FileDto>(null, ex.Message, StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error uploading file - FileName: {FileName} | UserId: {UserId} | TraceId: {TraceId}",
                    request?.File?.FileName, CurrentUserId, TraceId);

                return Failure<FileDto>(
                    null,
                    "An internal error occurred while uploading the file.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Download a file
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> DownloadFile([FromQuery] long id)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Unauthorized(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "User is not authorized.",
                        TraceId = TraceId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Get file info first
                var fileInfo = await _fileService.GetFileAsync(id, CurrentUserId.Value);
                if (fileInfo == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "File not found.",
                        TraceId = TraceId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Get file stream
                var stream = await _fileService.DownloadFileAsync(id, CurrentUserId.Value);

                // Set response headers
                _responseHeaderService.SetCustomHeader("Content-Disposition",
                    $"attachment; filename=\"{fileInfo.OriginalFileName}\"");
                _responseHeaderService.SetCustomHeader("Content-Length", fileInfo.FileSize.ToString());
                _responseHeaderService.SetCacheControl("private, max-age=3600");

                _logger.LogInformation(
                    "File downloaded - FileId: {FileId} | FileName: {FileName} | UserId: {UserId} | TraceId: {TraceId}",
                    id, fileInfo.OriginalFileName, CurrentUserId, TraceId);

                return File(stream, fileInfo.ContentType, fileInfo.OriginalFileName);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (UnauthorizedException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("expired"))
            {
                return StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error downloading file {FileId} - UserId: {UserId} | TraceId: {TraceId}",
                    id, CurrentUserId, TraceId);

                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An internal error occurred while downloading the file.",
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get file details
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<FileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<FileDto>>> GetFile([FromQuery] long id)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<FileDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var file = await _fileService.GetFileAsync(id, CurrentUserId.Value);

                _logger.LogInformation(
                    "File details retrieved - FileId: {FileId} | UserId: {UserId} | TraceId: {TraceId}",
                    id, CurrentUserId, TraceId);

                return Success(file, "File details retrieved successfully.");
            }
            catch (NotFoundException ex)
            {
                return Failure<FileDto>(null, ex.Message, StatusCodes.Status404NotFound);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<FileDto>(null, ex.Message, StatusCodes.Status403Forbidden);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting file {FileId} - UserId: {UserId} | TraceId: {TraceId}",
                    id, CurrentUserId, TraceId);

                return Failure<FileDto>(
                    null,
                    "An internal error occurred while getting the file.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get user's files with search and filters
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IPagedList<FileDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IPagedList<FileDto>>>> GetMyFiles([FromQuery] FileSearchDto searchDto)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IPagedList<FileDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                searchDto ??= new FileSearchDto();

                // Validate page size
                if (searchDto.PageSize > 100)
                {
                    searchDto.PageSize = 100;
                }

                var files = await _fileService.GetUserFilesAsync(CurrentUserId.Value, searchDto);

                // Set pagination headers
                if (files != null)
                {
                    _responseHeaderService.SetPaginationHeaders(new PaginationMetadata
                    {
                        CurrentPage = searchDto.PageNumber,
                        PageSize = files.PageSize,
                        TotalPages = files.TotalPages,
                        TotalCount = files.TotalCount,
                        HasNext = files.HasNextPage,
                        HasPrevious = files.HasPreviousPage
                    });

                    // Set cache control for list
                    _responseHeaderService.SetCacheControl("private, max-age=60");
                }

                _logger.LogInformation(
                    "User files retrieved - Count: {Count} | Page: {Page} | UserId: {UserId} | TraceId: {TraceId}",
                    files?.Count ?? 0, searchDto.PageNumber, CurrentUserId, TraceId);

                return Success(files, $"Retrieved {files?.Count ?? 0} files.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting files for user {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<IPagedList<FileDto>>(
                    null,
                    "An internal error occurred while getting files.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Update file information
        /// </summary>
        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<FileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<FileDto>>> UpdateFile(
            [FromQuery] long id,
            [FromBody] UpdateFileDto request)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<FileDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (!ModelState.IsValid)
                {
                    return Failure<FileDto>(
                        null,
                        "Invalid update data.",
                        StatusCodes.Status400BadRequest);
                }

                var updated = await _fileService.UpdateFileAsync(id, CurrentUserId.Value, request);

                _logger.LogInformation(
                    "File updated - FileId: {FileId} | UserId: {UserId} | TraceId: {TraceId}",
                    id, CurrentUserId, TraceId);

                return Success(updated, "File updated successfully.");
            }
            catch (NotFoundException ex)
            {
                return Failure<FileDto>(null, ex.Message, StatusCodes.Status404NotFound);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<FileDto>(null, ex.Message, StatusCodes.Status403Forbidden);
            }
            catch (ValidationException ex)
            {
                return Failure<FileDto>(null, ex.Message, StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating file {FileId} - UserId: {UserId} | TraceId: {TraceId}",
                    id, CurrentUserId, TraceId);

                return Failure<FileDto>(
                    null,
                    "An internal error occurred while updating the file.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteFile([FromQuery] long id)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<bool>(
                        false,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var deleted = await _fileService.DeleteFileAsync(id, CurrentUserId.Value);

                if (deleted)
                {
                    _logger.LogInformation(
                        "File deleted - FileId: {FileId} | UserId: {UserId} | TraceId: {TraceId}",
                        id, CurrentUserId, TraceId);

                    return Success(true, "File deleted successfully.");
                }

                return Failure<bool>(false, "Failed to delete file.", StatusCodes.Status400BadRequest);
            }
            catch (NotFoundException ex)
            {
                return Failure<bool>(false, ex.Message, StatusCodes.Status404NotFound);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<bool>(false, ex.Message, StatusCodes.Status403Forbidden);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error deleting file {FileId} - UserId: {UserId} | TraceId: {TraceId}",
                    id, CurrentUserId, TraceId);

                return Failure<bool>(
                    false,
                    "An internal error occurred while deleting the file.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Delete multiple files
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteMultipleFiles([FromBody] List<long> fileIds)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<bool>(
                        false,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (fileIds == null || !fileIds.Any())
                {
                    return Failure<bool>(
                        false,
                        "No files specified for deletion.",
                        StatusCodes.Status400BadRequest);
                }

                if (fileIds.Count > 50)
                {
                    return Failure<bool>(
                        false,
                        "Cannot delete more than 50 files at once.",
                        StatusCodes.Status400BadRequest);
                }

                var deleted = await _fileService.DeleteFilesAsync(fileIds, CurrentUserId.Value);

                if (deleted)
                {
                    _logger.LogInformation(
                        "Multiple files deleted - Count: {Count} | UserId: {UserId} | TraceId: {TraceId}",
                        fileIds.Count, CurrentUserId, TraceId);

                    return Success(true, $"{fileIds.Count} file(s) deleted successfully.");
                }

                return Success(false, "Some files could not be deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error deleting multiple files - Count: {Count} | UserId: {UserId} | TraceId: {TraceId}",
                    fileIds?.Count, CurrentUserId, TraceId);

                return Failure<bool>(
                    false,
                    "An internal error occurred while deleting files.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        #endregion

        #region Folder Operations

        /// <summary>
        /// Create a new folder
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<FolderDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ApiResponse<FolderDto>>> CreateFolder([FromBody] CreateFolderDto request)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<FolderDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (!ModelState.IsValid)
                {
                    return Failure<FolderDto>(
                        null,
                        "Invalid folder data.",
                        StatusCodes.Status400BadRequest);
                }

                var folder = await _fileService.CreateFolderAsync(CurrentUserId.Value, request);

                _logger.LogInformation(
                    "Folder created - FolderId: {FolderId} | FolderName: {FolderName} | UserId: {UserId} | TraceId: {TraceId}",
                    folder.FolderId, folder.FolderName, CurrentUserId, TraceId);

                _responseHeaderService.SetCustomHeader("Location", $"/api/files/getfolder?id={folder.FolderId}");

                return StatusCode(StatusCodes.Status201Created, new ApiResponse<FolderDto>
                {
                    Success = true,
                    Message = "Folder created successfully.",
                    Data = folder,
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Failure<FolderDto>(null, ex.Message, StatusCodes.Status409Conflict);
            }
            catch (NotFoundException ex)
            {
                return Failure<FolderDto>(null, ex.Message, StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating folder - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<FolderDto>(
                    null,
                    "An internal error occurred while creating the folder.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get user's folders
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<FolderDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<List<FolderDto>>>> GetFolders()
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<List<FolderDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var folders = await _fileService.GetUserFoldersAsync(CurrentUserId.Value);

                // Set cache control
                _responseHeaderService.SetCacheControl("private, max-age=300");

                _logger.LogInformation(
                    "Folders retrieved - Count: {Count} | UserId: {UserId} | TraceId: {TraceId}",
                    folders?.Count ?? 0, CurrentUserId, TraceId);

                return Success(folders, $"Retrieved {folders?.Count ?? 0} folders.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting folders for user {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<List<FolderDto>>(
                    null,
                    "An internal error occurred while getting folders.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Delete a folder
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteFolder([FromQuery] string folderId)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<bool>(
                        false,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (string.IsNullOrWhiteSpace(folderId))
                {
                    return Failure<bool>(
                        false,
                        "Folder ID is required.",
                        StatusCodes.Status400BadRequest);
                }

                var deleted = await _fileService.DeleteFolderAsync(folderId, CurrentUserId.Value);

                if (deleted)
                {
                    _logger.LogInformation(
                        "Folder deleted - FolderId: {FolderId} | UserId: {UserId} | TraceId: {TraceId}",
                        folderId, CurrentUserId, TraceId);

                    return Success(true, "Folder deleted successfully.");
                }

                return Failure<bool>(false, "Failed to delete folder.", StatusCodes.Status400BadRequest);
            }
            catch (NotFoundException ex)
            {
                return Failure<bool>(false, ex.Message, StatusCodes.Status404NotFound);
            }
            catch (InvalidOperationException ex)
            {
                return Failure<bool>(false, ex.Message, StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error deleting folder {FolderId} - UserId: {UserId} | TraceId: {TraceId}",
                    folderId, CurrentUserId, TraceId);

                return Failure<bool>(
                    false,
                    "An internal error occurred while deleting the folder.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Move file to a different folder
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<FileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<FileDto>>> MoveFile([FromBody] MoveFileDto request)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<FileDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (!ModelState.IsValid)
                {
                    return Failure<FileDto>(
                        null,
                        "Invalid move data.",
                        StatusCodes.Status400BadRequest);
                }

                var moved = await _fileService.MoveFileAsync(CurrentUserId.Value, request);

                _logger.LogInformation(
                    "File moved - FileId: {FileId} | TargetFolder: {FolderId} | UserId: {UserId} | TraceId: {TraceId}",
                    request.FileId, request.TargetFolderId, CurrentUserId, TraceId);

                return Success(moved, "File moved successfully.");
            }
            catch (NotFoundException ex)
            {
                return Failure<FileDto>(null, ex.Message, StatusCodes.Status404NotFound);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<FileDto>(null, ex.Message, StatusCodes.Status403Forbidden);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error moving file {FileId} - UserId: {UserId} | TraceId: {TraceId}",
                    request?.FileId, CurrentUserId, TraceId);

                return Failure<FileDto>(
                    null,
                    "An internal error occurred while moving the file.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        #endregion

        #region Sharing Operations

        /// <summary>
        /// Share a file
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<FileShareDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<FileShareDto>>> ShareFile([FromBody] CreateFileShareDto request)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<FileShareDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (!ModelState.IsValid)
                {
                    return Failure<FileShareDto>(
                        null,
                        "Invalid share data.",
                        StatusCodes.Status400BadRequest);
                }

                var share = await _fileService.ShareFileAsync(CurrentUserId.Value, request);

                _logger.LogInformation(
                    "File shared - FileId: {FileId} | ShareToken: {ShareToken} | UserId: {UserId} | TraceId: {TraceId}",
                    request.FileId, share.ShareToken, CurrentUserId, TraceId);

                _responseHeaderService.SetCustomHeader("Location", $"/api/files/downloadshared?token={share.ShareToken}");

                return StatusCode(StatusCodes.Status201Created, new ApiResponse<FileShareDto>
                {
                    Success = true,
                    Message = "File shared successfully.",
                    Data = share,
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (NotFoundException ex)
            {
                return Failure<FileShareDto>(null, ex.Message, StatusCodes.Status404NotFound);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<FileShareDto>(null, ex.Message, StatusCodes.Status403Forbidden);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sharing file {FileId} - UserId: {UserId} | TraceId: {TraceId}",
                    request?.FileId, CurrentUserId, TraceId);

                return Failure<FileShareDto>(
                    null,
                    "An internal error occurred while sharing the file.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get file shares
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<FileShareDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<List<FileShareDto>>>> GetFileShares([FromQuery] long fileId)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<List<FileShareDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var shares = await _fileService.GetFileSharesAsync(fileId, CurrentUserId.Value);

                _logger.LogInformation(
                    "File shares retrieved - FileId: {FileId} | Count: {Count} | UserId: {UserId} | TraceId: {TraceId}",
                    fileId, shares?.Count ?? 0, CurrentUserId, TraceId);

                return Success(shares, $"Retrieved {shares?.Count ?? 0} shares.");
            }
            catch (NotFoundException ex)
            {
                return Failure<List<FileShareDto>>(null, ex.Message, StatusCodes.Status404NotFound);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<List<FileShareDto>>(null, ex.Message, StatusCodes.Status403Forbidden);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting shares for file {FileId} - UserId: {UserId} | TraceId: {TraceId}",
                    fileId, CurrentUserId, TraceId);

                return Failure<List<FileShareDto>>(
                    null,
                    "An internal error occurred while getting file shares.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Revoke a file share
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<bool>>> RevokeShare([FromQuery] long shareId)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<bool>(
                        false,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var revoked = await _fileService.RevokeShareAsync(shareId, CurrentUserId.Value);

                if (revoked)
                {
                    _logger.LogInformation(
                        "File share revoked - ShareId: {ShareId} | UserId: {UserId} | TraceId: {TraceId}",
                        shareId, CurrentUserId, TraceId);

                    return Success(true, "File share revoked successfully.");
                }

                return Failure<bool>(false, "Failed to revoke share.", StatusCodes.Status400BadRequest);
            }
            catch (NotFoundException ex)
            {
                return Failure<bool>(false, ex.Message, StatusCodes.Status404NotFound);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<bool>(false, ex.Message, StatusCodes.Status403Forbidden);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error revoking share {ShareId} - UserId: {UserId} | TraceId: {TraceId}",
                    shareId, CurrentUserId, TraceId);

                return Failure<bool>(
                    false,
                    "An internal error occurred while revoking the share.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Download shared file (public access)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status410Gone)]
        public async Task<ActionResult> DownloadShared([FromQuery] string token, [FromQuery] string password = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Share token is required.",
                        TraceId = TraceId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Get file info first to get metadata
                var fileInfo = await _fileService.GetSharedFileInfoAsync(token, password);

                // Get file stream
                var stream = await _fileService.DownloadSharedFileAsync(token, password);

                // Set response headers
                _responseHeaderService.SetCustomHeader("Content-Disposition",
                    $"attachment; filename=\"{fileInfo.OriginalFileName}\"");
                _responseHeaderService.SetCustomHeader("Content-Length", fileInfo.FileSize.ToString());

                _logger.LogInformation(
                    "Shared file downloaded - Token: {Token} | IP: {IpAddress} | TraceId: {TraceId}",
                    token, ClientV4IpAddress, TraceId);

                return File(stream, fileInfo.ContentType, fileInfo.OriginalFileName);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("expired"))
            {
                return StatusCode(StatusCodes.Status410Gone, new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message,
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error downloading shared file {Token} | TraceId: {TraceId}",
                    token, TraceId);

                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An internal error occurred while downloading the shared file.",
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        #endregion

        #region Statistics and Logs

        /// <summary>
        /// Get user storage statistics
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<FileStatisticsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<FileStatisticsDto>>> GetStatistics()
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<FileStatisticsDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var stats = await _fileService.GetUserStatisticsAsync(CurrentUserId.Value);

                // Set cache control for statistics
                _responseHeaderService.SetCacheControl("private, max-age=60");

                _logger.LogInformation(
                    "Statistics retrieved - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Success(stats, "Statistics retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting statistics for user {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<FileStatisticsDto>(
                    null,
                    "An internal error occurred while getting statistics.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get file access logs
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<FileAccessDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<List<FileAccessDto>>>> GetFileAccessLogs([FromQuery] long fileId)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<List<FileAccessDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var logs = await _fileService.GetFileAccessLogsAsync(fileId, CurrentUserId.Value);

                _logger.LogInformation(
                    "Access logs retrieved - FileId: {FileId} | Count: {Count} | UserId: {UserId} | TraceId: {TraceId}",
                    fileId, logs?.Count ?? 0, CurrentUserId, TraceId);

                return Success(logs, $"Retrieved {logs?.Count ?? 0} access logs.");
            }
            catch (NotFoundException ex)
            {
                return Failure<List<FileAccessDto>>(null, ex.Message, StatusCodes.Status404NotFound);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<List<FileAccessDto>>(null, ex.Message, StatusCodes.Status403Forbidden);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting access logs for file {FileId} - UserId: {UserId} | TraceId: {TraceId}",
                    fileId, CurrentUserId, TraceId);

                return Failure<List<FileAccessDto>>(
                    null,
                    "An internal error occurred while getting access logs.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Perform bulk operations on files
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<bool>>> BulkOperation([FromBody] BulkFileOperationDto request)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<bool>(
                        false,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (!ModelState.IsValid || request?.FileIds == null || !request.FileIds.Any())
                {
                    return Failure<bool>(
                        false,
                        "Invalid bulk operation data.",
                        StatusCodes.Status400BadRequest);
                }

                if (request.FileIds.Count > 100)
                {
                    return Failure<bool>(
                        false,
                        "Cannot process more than 100 files at once.",
                        StatusCodes.Status400BadRequest);
                }

                var result = await _fileService.BulkOperationAsync(CurrentUserId.Value, request);

                _logger.LogInformation(
                    "Bulk operation performed - Operation: {Operation} | FileCount: {Count} | UserId: {UserId} | TraceId: {TraceId}",
                    request.Operation, request.FileIds.Count, CurrentUserId, TraceId);

                return Success(result, $"Bulk {request.Operation} completed successfully.");
            }
            catch (ValidationException ex)
            {
                return Failure<bool>(false, ex.Message, StatusCodes.Status400BadRequest);
            }
            catch (NotImplementedException ex)
            {
                return Failure<bool>(false, ex.Message, StatusCodes.Status501NotImplemented);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error performing bulk operation - Operation: {Operation} | UserId: {UserId} | TraceId: {TraceId}",
                    request?.Operation, CurrentUserId, TraceId);

                return Failure<bool>(
                    false,
                    "An internal error occurred while performing the bulk operation.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Check storage availability
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<bool>>> CheckStorageAvailability([FromQuery] long fileSize)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<bool>(
                        false,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (fileSize <= 0)
                {
                    return Failure<bool>(
                        false,
                        "Invalid file size.",
                        StatusCodes.Status400BadRequest);
                }

                var hasSpace = await _fileService.ValidateUserStorageAsync(CurrentUserId.Value, fileSize);

                if (hasSpace)
                {
                    return Success(true, "Sufficient storage available.");
                }

                return Success(false, "Insufficient storage space.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error checking storage availability - UserId: {UserId} | FileSize: {FileSize} | TraceId: {TraceId}",
                    CurrentUserId, fileSize, TraceId);

                return Failure<bool>(
                    false,
                    "An internal error occurred while checking storage availability.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        #endregion
    }
}
