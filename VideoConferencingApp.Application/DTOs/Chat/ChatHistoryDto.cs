namespace VideoConferencingApp.Application.DTOs.Chat
{
    public class ChatHistoryDto
    {
        public string? UserId { get; set; }
        public string? GroupId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

}