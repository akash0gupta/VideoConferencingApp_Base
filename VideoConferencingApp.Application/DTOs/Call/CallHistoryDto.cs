using Microsoft.VisualBasic;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Call
{
    public class CallHistoryDto
    {
        public string CallId { get; set; } = string.Empty;
        public string? OtherUserId { get; set; }
        public string? OtherUserName { get; set; }
        public string? OtherUserAvatar { get; set; }
        public CallsType Type { get; set; }
        public CallStatus Status { get; set; }
        public CallDirection Direction { get; set; }
        public DateTime InitiatedAt { get; set; }
        public DateTime? ConnectedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int DurationSeconds { get; set; }
        public string? EndReason { get; set; }
    }
}