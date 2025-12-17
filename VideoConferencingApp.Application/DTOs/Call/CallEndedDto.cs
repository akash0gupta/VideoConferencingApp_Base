namespace VideoConferencingApp.Application.DTOs.Call
{
    public class CallEndedDto
    {
        public string CallId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime EndedAt { get; set; } = DateTime.UtcNow;
        public int DurationSeconds { get; set; }
        public string? Reason { get; set; }
    }
}


