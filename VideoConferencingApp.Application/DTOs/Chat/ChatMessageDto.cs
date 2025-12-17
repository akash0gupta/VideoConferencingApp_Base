using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Chat
{
    public class ChatMessageDto
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatar { get; set; }
        public string? ReceiverId { get; set; }
        public string? GroupId { get; set; }
        public string Content { get; set; } = string.Empty;
        public MessageType Type { get; set; } = MessageType.Text;
        public MessageStatus Status { get; set; } = MessageStatus.Sending;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? ReplyToMessageId { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}