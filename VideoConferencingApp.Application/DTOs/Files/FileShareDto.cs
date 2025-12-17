using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Files
{
    public class FileShareDto
        {
            public long Id { get; set; }
            public long FileId { get; set; }
            public string FileName { get; set; }
            public long SharedById { get; set; }
            public string SharedByName { get; set; }
            public long? SharedWithUserId { get; set; }
            public string SharedWithUserName { get; set; }
            public string ShareToken { get; set; }
            public string ShareUrl { get; set; }
            public FilePermission Permission { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ExpiresAt { get; set; }
            public bool IsPasswordProtected { get; set; }
            public int AccessCount { get; set; }
            public int? MaxAccessCount { get; set; }
        }
    
}
