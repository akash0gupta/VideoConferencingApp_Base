using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Call
{
    public class WebRtcSignalDto
    {
        public string CallId { get; set; } = string.Empty;
        public string FromUserId { get; set; } = string.Empty;
        public string ToUserId { get; set; } = string.Empty;
        public SignalType Type { get; set; }
        public object? Signal { get; set; }
    }
}