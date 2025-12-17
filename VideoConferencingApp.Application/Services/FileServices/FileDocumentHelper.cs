using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.Services.FileServices
{
    public static class FileDocumentHelper
    {
        #region MIME Types Dictionary

        private static readonly Dictionary<string, string> MimeTypes = new()
    {
        // Documents
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".ppt", "application/vnd.ms-powerpoint" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        { ".odt", "application/vnd.oasis.opendocument.text" },
        { ".ods", "application/vnd.oasis.opendocument.spreadsheet" },
        { ".odp", "application/vnd.oasis.opendocument.presentation" },
        { ".rtf", "application/rtf" },
        
        // Text
        { ".txt", "text/plain" },
        { ".csv", "text/csv" },
        { ".xml", "text/xml" },
        { ".json", "application/json" },
        { ".html", "text/html" },
        { ".htm", "text/html" },
        { ".css", "text/css" },
        { ".js", "application/javascript" },
        { ".md", "text/markdown" },
        
        // Images
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".webp", "image/webp" },
        { ".svg", "image/svg+xml" },
        { ".ico", "image/x-icon" },
        { ".tiff", "image/tiff" },
        { ".tif", "image/tiff" },
        
        // Videos
        { ".mp4", "video/mp4" },
        { ".avi", "video/x-msvideo" },
        { ".mov", "video/quicktime" },
        { ".wmv", "video/x-ms-wmv" },
        { ".flv", "video/x-flv" },
        { ".mkv", "video/x-matroska" },
        { ".webm", "video/webm" },
        { ".m4v", "video/x-m4v" },
        { ".mpg", "video/mpeg" },
        { ".mpeg", "video/mpeg" },
        
        // Audio
        { ".mp3", "audio/mpeg" },
        { ".wav", "audio/wav" },
        { ".ogg", "audio/ogg" },
        { ".m4a", "audio/x-m4a" },
        { ".aac", "audio/aac" },
        { ".flac", "audio/flac" },
        { ".wma", "audio/x-ms-wma" },
        
        // Archives
        { ".zip", "application/zip" },
        { ".rar", "application/vnd.rar" },
        { ".7z", "application/x-7z-compressed" },
        { ".tar", "application/x-tar" },
        { ".gz", "application/gzip" },
        { ".bz2", "application/x-bzip2" },
        
        // Other
        { ".exe", "application/x-msdownload" },
        { ".msi", "application/x-msi" },
        { ".apk", "application/vnd.android.package-archive" },
        { ".dmg", "application/x-apple-diskimage" },
        { ".iso", "application/x-iso9660-image" },
        { ".torrent", "application/x-bittorrent" }
    };

        #endregion

        #region File Categories

        private static readonly Dictionary<FileCategory, HashSet<string>> FileCategories = new()
        {
            [FileCategory.Document] = new HashSet<string>
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".odt", ".ods", ".odp", ".rtf", ".txt", ".csv"
        },
            [FileCategory.Image] = new HashSet<string>
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
            ".svg", ".ico", ".tiff", ".tif"
        },
            [FileCategory.Video] = new HashSet<string>
        {
            ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv",
            ".webm", ".m4v", ".mpg", ".mpeg"
        },
            [FileCategory.Audio] = new HashSet<string>
        {
            ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".flac", ".wma"
        },
            [FileCategory.Archive] = new HashSet<string>
        {
            ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2"
        },
            [FileCategory.Code] = new HashSet<string>
        {
            ".html", ".htm", ".css", ".js", ".json", ".xml", ".md",
            ".cs", ".java", ".py", ".cpp", ".c", ".h", ".php", ".rb", ".go"
        }
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Get MIME type for a file extension
        /// </summary>
        public static string GetMimeType(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return "application/octet-stream";

            extension = extension.ToLowerInvariant();
            if (!extension.StartsWith("."))
                extension = "." + extension;

            return MimeTypes.ContainsKey(extension)
                ? MimeTypes[extension]
                : "application/octet-stream";
        }

        /// <summary>
        /// Get MIME type from file path
        /// </summary>
        public static string GetMimeTypeFromPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return "application/octet-stream";

            var extension = Path.GetExtension(filePath);
            return GetMimeType(extension);
        }

        /// <summary>
        /// Get file category based on extension
        /// </summary>
        public static FileCategory GetFileCategory(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return FileCategory.Other;

            extension = extension.ToLowerInvariant();
            if (!extension.StartsWith("."))
                extension = "." + extension;

            foreach (var category in FileCategories)
            {
                if (category.Value.Contains(extension))
                    return category.Key;
            }

            return FileCategory.Other;
        }

        /// <summary>
        /// Check if file extension is allowed
        /// </summary>
        public static bool IsAllowedExtension(string extension, string[] allowedExtensions)
        {
            if (string.IsNullOrEmpty(extension) || allowedExtensions == null || !allowedExtensions.Any())
                return false;

            extension = extension.ToLowerInvariant();
            if (!extension.StartsWith("."))
                extension = "." + extension;

            return allowedExtensions.Select(e => e.ToLowerInvariant()).Contains(extension);
        }

        /// <summary>
        /// Check if file is an image
        /// </summary>
        public static bool IsImage(string extension)
        {
            return GetFileCategory(extension) == FileCategory.Image;
        }

        /// <summary>
        /// Check if file is a document
        /// </summary>
        public static bool IsDocument(string extension)
        {
            return GetFileCategory(extension) == FileCategory.Document;
        }

        /// <summary>
        /// Check if file is a video
        /// </summary>
        public static bool IsVideo(string extension)
        {
            return GetFileCategory(extension) == FileCategory.Video;
        }

        /// <summary>
        /// Check if file is audio
        /// </summary>
        public static bool IsAudio(string extension)
        {
            return GetFileCategory(extension) == FileCategory.Audio;
        }

        /// <summary>
        /// Check if file is an archive
        /// </summary>
        public static bool IsArchive(string extension)
        {
            return GetFileCategory(extension) == FileCategory.Archive;
        }

        /// <summary>
        /// Check if file can be previewed in browser
        /// </summary>
        public static bool CanPreview(string extension)
        {
            var previewableCategories = new[]
            {
            FileCategory.Image,
            FileCategory.Document
        };

            var category = GetFileCategory(extension);
            if (previewableCategories.Contains(category))
                return true;

            // Additional previewable types
            var previewableExtensions = new[] { ".pdf", ".txt", ".md", ".json", ".xml" };
            extension = extension.ToLowerInvariant();

            return previewableExtensions.Contains(extension);
        }

        /// <summary>
        /// Format file size to human readable string
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Generate unique filename
        /// </summary>
        public static string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);

            return $"{fileNameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
        }

        /// <summary>
        /// Sanitize filename for safe storage
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "unnamed";

            // Remove invalid characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

            // Remove additional problematic characters
            sanitized = Regex.Replace(sanitized, @"[^\w\s\-\.]", "_");
            sanitized = Regex.Replace(sanitized, @"\.{2,}", ".");
            sanitized = Regex.Replace(sanitized, @"_{2,}", "_");
            sanitized = sanitized.Trim('_', '.', ' ');

            // Ensure filename is not too long
            if (sanitized.Length > 255)
            {
                var extension = Path.GetExtension(sanitized);
                var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
                sanitized = nameWithoutExt.Substring(0, 255 - extension.Length) + extension;
            }

            return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
        }

        /// <summary>
        /// Get file icon class based on extension (for UI)
        /// </summary>
        public static string GetFileIconClass(string extension)
        {
            var category = GetFileCategory(extension);

            return category switch
            {
                FileCategory.Document => "fa-file-text",
                FileCategory.Image => "fa-file-image",
                FileCategory.Video => "fa-file-video",
                FileCategory.Audio => "fa-file-audio",
                FileCategory.Archive => "fa-file-archive",
                FileCategory.Code => "fa-file-code",
                _ => "fa-file"
            };
        }

        /// <summary>
        /// Calculate file hash (SHA256)
        /// </summary>
        public static async Task<string> CalculateFileHashAsync(Stream stream)
        {
            using (var sha256 = SHA256.Create())
            {
                var hash = await sha256.ComputeHashAsync(stream);
                stream.Position = 0; // Reset stream position
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Check if file size is within limit
        /// </summary>
        public static bool IsFileSizeValid(long fileSize, long maxSizeInBytes)
        {
            return fileSize > 0 && fileSize <= maxSizeInBytes;
        }

        /// <summary>
        /// Get extension from MIME type
        /// </summary>
        public static string GetExtensionFromMimeType(string mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
                return string.Empty;

            var kvp = MimeTypes.FirstOrDefault(x => x.Value.Equals(mimeType, StringComparison.OrdinalIgnoreCase));
            return kvp.Key ?? string.Empty;
        }

        /// <summary>
        /// Validate file for upload
        /// </summary>
        public static ValidationResult ValidateFile(IFormFile file, FileUploadSettings settings)
        {
            var result = new ValidationResult { IsValid = true, Errors = new List<string>() };

            // Check if file exists
            if (file == null || file.Length == 0)
            {
                result.IsValid = false;
                result.Errors.Add("File is empty or missing");
                return result;
            }

            // Check file size
            if (file.Length > settings.MaxFileSize)
            {
                result.IsValid = false;
                result.Errors.Add($"File size exceeds maximum allowed size of {FormatFileSize(settings.MaxFileSize)}");
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName);
            if (!IsAllowedExtension(extension, settings.AllowedExtensions))
            {
                result.IsValid = false;
                result.Errors.Add($"File type '{extension}' is not allowed");
            }

            // Check MIME type
            if (settings.ValidateMimeType)
            {
                var expectedMimeType = GetMimeType(extension);
                if (!file.ContentType.Equals(expectedMimeType, StringComparison.OrdinalIgnoreCase))
                {
                    result.IsValid = false;
                    result.Errors.Add("File content type does not match extension");
                }
            }

            return result;
        }

        /// <summary>
        /// Extract metadata from file
        /// </summary>
        public static FileMetadata ExtractMetadata(IFormFile file)
        {
            return new FileMetadata
            {
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType,
                Extension = Path.GetExtension(file.FileName),
                Category = GetFileCategory(Path.GetExtension(file.FileName)),
                UploadedAt = DateTime.UtcNow
            };
        }

        #endregion
    }

    #region Supporting Classes and Enums

    public enum FileCategory
    {
        Document,
        Image,
        Video,
        Audio,
        Archive,
        Code,
        Other
    }

    public class FileUploadSettings
    {
        public long MaxFileSize { get; set; } = 104857600; // 100MB default
        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
        public bool ValidateMimeType { get; set; } = true;
        public bool GenerateUniqueName { get; set; } = true;
        public bool SanitizeFileName { get; set; } = true;
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string ErrorMessage => string.Join("; ", Errors);
    }

    public class FileMetadata
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public string Extension { get; set; }
        public FileCategory Category { get; set; }
        public DateTime UploadedAt { get; set; }
        public Dictionary<string, object> CustomMetadata { get; set; } = new Dictionary<string, object>();
    }

    #endregion
}
