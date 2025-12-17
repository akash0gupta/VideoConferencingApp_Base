using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Domain.Exceptions;

namespace VideoConferencingApp.Application.Services.FileServices
{
    public class FileManagerService : IFileManagerService
    {
        private readonly IFileStorageService _storageService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileManagerService> _logger;
        private readonly bool _useAzureStorage;
        private readonly string _containerName;
        private readonly string _cdnUrl;

        public FileManagerService(
            IFileStorageService storageService,
            IConfiguration configuration,
            ILogger<FileManagerService> logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _useAzureStorage = _configuration.GetValue<bool>("Storage:UseAzureStorage");
            _containerName = _configuration["Storage:Azure:ContainerName"] ?? "files";
            _cdnUrl = _configuration["Storage:CdnUrl"];
        }

        #region Core File Operations

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, string folderPath = null)
        {
            try
            {
                // Sanitize filename
                fileName = FileDocumentHelper.SanitizeFileName(fileName);

                // Generate unique filename to avoid conflicts
                var uniqueFileName = FileDocumentHelper.GenerateUniqueFileName(fileName);

                // Build full path
                var fullPath = string.IsNullOrEmpty(folderPath)
                    ? uniqueFileName
                    : Path.Combine(folderPath, uniqueFileName).Replace("\\", "/");

                // Save using the appropriate storage service
                var savedPath = await _storageService.SaveFileAsync(fileStream, fullPath, contentType);

                _logger.LogInformation("File saved successfully: {FileName} -> {SavedPath}", fileName, savedPath);

                return savedPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file: {FileName}", fileName);
                throw new FileOperationException($"Failed to save file: {fileName}", ex);
            }
        }

        public async Task<Stream> GetFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    throw new ArgumentException("File path cannot be empty");

                var stream = await _storageService.GetFileAsync(filePath);

                _logger.LogInformation("File retrieved: {FilePath}", filePath);

                return stream;
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file: {FilePath}", filePath);
                throw new FileOperationException($"Failed to retrieve file: {filePath}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return false;

                await _storageService.DeleteFileAsync(filePath);

                _logger.LogInformation("File deleted: {FilePath}", filePath);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return false;

                // Try to get the file, if it throws FileNotFoundException, it doesn't exist
                try
                {
                    using var stream = await _storageService.GetFileAsync(filePath);
                    return true;
                }
                catch (FileNotFoundException)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking file existence: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<FileInfo> GetFileInfoAsync(string filePath)
        {
            try
            {
                // For Azure storage, we need to get blob properties
                // For local storage, we can use System.IO.FileInfo
                // This is a simplified implementation

                var exists = await FileExistsAsync(filePath);
                if (!exists)
                    throw new FileNotFoundException($"File not found: {filePath}");

                // You might need to enhance this based on your storage implementation
                return new FileInfo(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file info: {FilePath}", filePath);
                throw;
            }
        }

        public async Task<string> CopyFileAsync(string sourcePath, string destinationPath)
        {
            try
            {
                using var sourceStream = await GetFileAsync(sourcePath);
                var contentType = FileDocumentHelper.GetMimeTypeFromPath(sourcePath);

                return await SaveFileAsync(sourceStream, Path.GetFileName(destinationPath), contentType, Path.GetDirectoryName(destinationPath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file from {Source} to {Destination}", sourcePath, destinationPath);
                throw new FileOperationException($"Failed to copy file from {sourcePath} to {destinationPath}", ex);
            }
        }

        public async Task<string> MoveFileAsync(string sourcePath, string destinationPath)
        {
            try
            {
                var copiedPath = await CopyFileAsync(sourcePath, destinationPath);
                await DeleteFileAsync(sourcePath);
                return copiedPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving file from {Source} to {Destination}", sourcePath, destinationPath);
                throw new FileOperationException($"Failed to move file from {sourcePath} to {destinationPath}", ex);
            }
        }

        #endregion

        #region Batch Operations

        public async Task<List<string>> SaveMultipleFilesAsync(Dictionary<Stream, string> files, string folderPath = null)
        {
            var savedPaths = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    var contentType = FileDocumentHelper.GetMimeTypeFromPath(file.Value);
                    var path = await SaveFileAsync(file.Key, file.Value, contentType, folderPath);
                    savedPaths.Add(path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving file in batch: {FileName}", file.Value);
                    // Continue with other files
                }
            }

            return savedPaths;
        }

        public async Task<bool> DeleteMultipleFilesAsync(List<string> filePaths)
        {
            var allDeleted = true;

            foreach (var path in filePaths)
            {
                var deleted = await DeleteFileAsync(path);
                if (!deleted)
                    allDeleted = false;
            }

            return allDeleted;
        }

        #endregion

        #region Storage Info

        public async Task<long> GetFileSizeAsync(string filePath)
        {
            try
            {
                using var stream = await GetFileAsync(filePath);
                return stream.Length;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file size: {FilePath}", filePath);
                return 0;
            }
        }

        public async Task<string> GenerateThumbnailAsync(string filePath, int width, int height)
        {
            try
            {
                // This would use an image processing library like ImageSharp
                // Simplified implementation
                var extension = Path.GetExtension(filePath);
                if (!FileDocumentHelper.IsImage(extension))
                    return null;

                // Generate thumbnail logic here
                var thumbnailName = $"thumb_{width}x{height}_{Path.GetFileName(filePath)}";
                var thumbnailPath = Path.Combine("thumbnails", thumbnailName);

                // Actual thumbnail generation would go here

                return thumbnailPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating thumbnail for: {FilePath}", filePath);
                return null;
            }
        }

        public async Task<string> GetFileUrlAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                    return null;

                // If using CDN, prepend CDN URL
                if (!string.IsNullOrEmpty(_cdnUrl))
                {
                    return $"{_cdnUrl.TrimEnd('/')}/{filePath.TrimStart('/')}";
                }

                // For Azure storage, the path itself might be a full URL
                if (_useAzureStorage && Uri.IsWellFormedUriString(filePath, UriKind.Absolute))
                {
                    return filePath;
                }

                // For local storage, return a relative path
                return $"/files/{filePath.TrimStart('/')}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file URL: {FilePath}", filePath);
                return null;
            }
        }

        #endregion
    }
}
