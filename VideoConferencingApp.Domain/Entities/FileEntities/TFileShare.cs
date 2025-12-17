using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Entities.FileEntities
{
    public class TFileShare : BaseEntity
    {
        public long FileId { get; set; }
        public long SharedById { get; set; }
        public long? SharedWithUserId { get; set; }
        public string ShareToken { get; set; } // For public sharing
        public FilePermission Permission { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Password { get; set; } // Optional password protection
        public int AccessCount { get; set; }
        public int? MaxAccessCount { get; set; }
    }

}
