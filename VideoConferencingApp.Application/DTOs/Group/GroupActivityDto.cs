using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Group
{
    public class GroupActivityDto
        {
            public long Id { get; set; }
            public GroupActivityType Type { get; set; }
            public string Description { get; set; } = string.Empty;
            public string? UserId { get; set; }
            public string? Username { get; set; }
            public string? TargetUserId { get; set; }
            public string? TargetUsername { get; set; }
            public DateTime OccurredAt { get; set; }
        }
       
}