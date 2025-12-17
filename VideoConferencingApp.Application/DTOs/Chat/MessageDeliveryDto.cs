using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Chat
{
    public class MessageDeliveryDto
    {
        public string MessageId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public MessageStatus Status { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}