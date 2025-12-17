using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Chat
{
    public class ConversationDto
    {
        public string Id { get; set; } = string.Empty;
        public ConversationType Type { get; set; }
        public string? UserId { get; set; }
        public string? GroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public ChatMessageDto? LastMessage { get; set; }
        public int UnreadCount { get; set; }
        public DateTime LastActivityAt { get; set; }
        public bool IsPinned { get; set; }
        public bool IsMuted { get; set; }
    }


}