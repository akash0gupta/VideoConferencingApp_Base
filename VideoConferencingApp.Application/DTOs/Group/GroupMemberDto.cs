using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Group
{
    public class GroupMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public GroupRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}