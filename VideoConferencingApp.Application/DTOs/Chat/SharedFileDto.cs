namespace VideoConferencingApp.Application.DTOs.Chat
{
    public class SharedFileDto
    {
        public string FileId { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string? SenderAvatar { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public DateTime SharedAt { get; set; }
    }


}