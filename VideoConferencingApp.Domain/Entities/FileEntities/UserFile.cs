using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Entities.FileEntities
{
    public class UserFile : BaseEntity, ISoftDeletedEntity
    {
        public long UserId { get; set; }
        public string FileName { get; set; }
        public string OriginalFileName { get; set; }
        public string FileExtension { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public string FilePath { get; set; }
        public string FileHash { get; set; } // SHA256 hash for duplicate detection
        public string ThumbnailPath { get; set; } // For images/videos
        public FileVisibility Visibility { get; set; }
        public string Description { get; set; }
        public string Tags { get; set; } // Comma-separated tags
        public string FolderId { get; set; }
        public int DownloadCount { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsEncrypted { get; set; }
        public string EncryptionKey { get; set; } // Encrypted with user's key
        public bool IsDeleted { get; set; }

    }

}
