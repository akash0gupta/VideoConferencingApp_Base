using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Entities.ChatEntities
{
    public class GroupMember : BaseEntity,ISoftDeletedEntity
    {
        public string GroupId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public GroupRole Role { get; set; } = GroupRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastReadAt { get; set; }
        public bool IsDeleted { get; set; }
        public long DeletedBy { get; set; }
    }
}