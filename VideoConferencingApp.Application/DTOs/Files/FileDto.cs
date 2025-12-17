using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Files
{
    public class FileDto
        {
            public long Id { get; set; }
            public long UserId { get; set; }
            public string UserName { get; set; }
            public string FileName { get; set; }
            public string OriginalFileName { get; set; }
            public string FileExtension { get; set; }
            public string ContentType { get; set; }
            public long FileSize { get; set; }
            public string FormattedFileSize { get; set; }
            public string ThumbnailUrl { get; set; }
            public FileVisibility Visibility { get; set; }
            public string Description { get; set; }
            public List<string> Tags { get; set; }
            public string FolderId { get; set; }
            public string FolderPath { get; set; }
            public int DownloadCount { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? LastAccessedAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public bool IsEncrypted { get; set; }
            public bool CanDownload { get; set; }
            public bool CanEdit { get; set; }
            public bool CanDelete { get; set; }
            public bool CanShare { get; set; }
            public string DownloadUrl { get; set; }
            public string ShareUrl { get; set; }
            public List<FileShareDto> Shares { get; set; }
        }
    
}
