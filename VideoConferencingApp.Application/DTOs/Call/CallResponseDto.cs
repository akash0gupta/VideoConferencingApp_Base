namespace VideoConferencingApp.Application.DTOs.Call
{
    public class CallResponseDto
    {
        public string CallId { get; set; } = string.Empty;
        public bool Accepted { get; set; }
        public string? Reason { get; set; }
    }
}