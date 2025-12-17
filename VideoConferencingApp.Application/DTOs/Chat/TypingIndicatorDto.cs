namespace VideoConferencingApp.Application.DTOs.Chat
{
    public class TypingIndicatorDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? ChatId { get; set; }
        public string? GroupId { get; set; }
        public bool IsTyping { get; set; }
    }
}