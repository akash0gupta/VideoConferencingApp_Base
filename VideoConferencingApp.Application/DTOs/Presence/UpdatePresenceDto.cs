using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Presence
{
    public class UpdatePresenceDto
    {
        public UserPresenceStatus Status { get; set; }
        public string? CustomMessage { get; set; }
    }
}