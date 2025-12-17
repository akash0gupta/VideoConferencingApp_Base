namespace VideoConferencingApp.Application.DTOs.Chat
{
    public class MarkMessagesReadDto
    {
        public List<string> MessageIds { get; set; } = new();
        public string? ChatId { get; set; }
        public string? GroupId { get; set; }
    }
}