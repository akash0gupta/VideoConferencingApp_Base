namespace VideoConferencingApp.Application.DTOs.Group
{
    public class CreateGroupDto
    {
        public string GroupName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> MemberIds { get; set; } = new();
    }
}