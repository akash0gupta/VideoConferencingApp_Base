using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Presence
{
    public class UserPresenceDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public UserPresenceStatus Status { get; set; }
        public string? CustomMessage { get; set; }
        public DateTime? LastSeen { get; set; }
        public bool IsOnline { get; set; }
    }
}