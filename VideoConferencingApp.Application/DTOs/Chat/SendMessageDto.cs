using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Chat
{
    public class SendMessageDto
    {
        public string? ReceiverId { get; set; }
        public string? GroupId { get; set; }
        public string Content { get; set; } = string.Empty;
        public MessageType Type { get; set; } = MessageType.Text;
        public string? ReplyToMessageId { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}