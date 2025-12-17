using Microsoft.VisualBasic;

namespace VideoConferencingApp.Application.DTOs.Call
{
    public class CallInitiatedDto
    {
        public string CallId { get; set; } = string.Empty;
        public string CallerId { get; set; } = string.Empty;
        public string CallerName { get; set; } = string.Empty;
        public string? CallerAvatar { get; set; }
        public string? ReceiverId { get; set; }
        public string? GroupId { get; set; }
        public CallType CallType { get; set; }
        public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    }
}