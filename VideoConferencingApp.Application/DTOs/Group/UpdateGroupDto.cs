namespace VideoConferencingApp.Application.DTOs.Group
{
    public class UpdateGroupDto
    {
        public string GroupId { get; set; } = string.Empty;
        public string? GroupName { get; set; }
        public string? Description { get; set; }
    }
}