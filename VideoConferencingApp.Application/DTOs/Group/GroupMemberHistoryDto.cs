using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Group
{
    public class GroupMemberHistoryDto
        {
            public string UserId { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string? Avatar { get; set; }
            public GroupMemberAction Action { get; set; }
            public DateTime ActionDate { get; set; }
            public string? ActionBy { get; set; }
        }
       
}