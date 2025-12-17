using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Entities.ChatEntities
{
    public class Message : BaseEntity,ISoftDeletedEntity
    {
        public string SenderId { get; set; } = string.Empty;
        public string? ReceiverId { get; set; }
        public string? GroupId { get; set; }
        public string Content { get; set; } = string.Empty;
        public MessageType Type { get; set; } = MessageType.Text;
        public MessageStatus Status { get; set; } = MessageStatus.Sent;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? ReplyToMessageId { get; set; }
        public string? Metadata { get; set; } // JSON for attachments, location, etc.

        // Navigation
        public bool IsDeleted { get; set; }
    }
}