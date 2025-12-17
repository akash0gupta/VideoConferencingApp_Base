using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.DTOs.Notification
{
    public class EmailAttachment
    {
        public string FileName { get; set; }
        public Stream FileStream { get; set; }
        public string ContentType { get; set; }
        public byte[] FileBytes { get; set; }
        public string ContentId { get; set; } // For inline attachments

        // Helper method to create attachment from bytes
        public static EmailAttachment FromBytes(string fileName, byte[] fileBytes, string contentType = null)
        {
            return new EmailAttachment
            {
                FileName = fileName,
                FileBytes = fileBytes,
                ContentType = contentType ?? GetContentType(fileName)
            };
        }

        // Helper method to create attachment from file path
        public static async Task<EmailAttachment> FromFilePathAsync(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var contentType = GetContentType(filePath);

            return FromBytes(fileName, fileBytes, contentType);
        }

        // Helper method for creating inline image
        public static EmailAttachment InlineImage(string fileName, byte[] imageBytes, string contentId)
        {
            return new EmailAttachment
            {
                FileName = fileName,
                FileBytes = imageBytes,
                ContentType = GetContentType(fileName),
                ContentId = contentId
            };
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".html" or ".htm" => "text/html",
                ".xml" => "text/xml",
                ".json" => "application/json",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".7z" => "application/x-7z-compressed",
                ".mp3" => "audio/mpeg",
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                _ => "application/octet-stream"
            };
        }
    }
}
