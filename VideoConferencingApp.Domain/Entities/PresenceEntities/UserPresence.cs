using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Entities.PresenceEntities
{
    public class UserPresence : BaseEntity,ISoftDeletedEntity
    {
        public string UserId { get; set; } = string.Empty;
        public UserPresenceStatus Status { get; set; } = UserPresenceStatus.Offline;
        public string? CustomMessage { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public DateTime? StatusChangedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}