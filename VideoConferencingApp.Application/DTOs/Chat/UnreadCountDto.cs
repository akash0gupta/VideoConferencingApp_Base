namespace VideoConferencingApp.Application.DTOs.Chat
{
    public class UnreadCountDto
    {
        public int TotalUnread { get; set; }
        public Dictionary<string, int> UnreadByConversation { get; set; } = new();
    }


}