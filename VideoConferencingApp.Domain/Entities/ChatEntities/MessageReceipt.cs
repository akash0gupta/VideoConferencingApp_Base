using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Entities.ChatEntities
{
    public class MessageReceipt : BaseEntity
    {
        public string MessageId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public MessageStatus Status { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    }
}