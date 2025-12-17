namespace VideoConferencingApp.Domain.Entities.ChatEntities
{
    public class ChatGroup : BaseEntity,ISoftDeletedEntity
    {
        public string GroupName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AvatarUrl { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
        public long DeletedBy { get; set; }
    }
}