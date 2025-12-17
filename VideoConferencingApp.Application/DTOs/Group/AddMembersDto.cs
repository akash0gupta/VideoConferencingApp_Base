namespace VideoConferencingApp.Application.DTOs.Group
{
    public class AddMembersDto
    {
        public string GroupId { get; set; } = string.Empty;
        public List<string> MemberIds { get; set; } = new();
    }
}