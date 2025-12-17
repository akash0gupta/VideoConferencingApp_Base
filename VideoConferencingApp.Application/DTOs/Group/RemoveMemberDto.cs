namespace VideoConferencingApp.Application.DTOs.Group
{
    public class RemoveMemberDto
    {
        public string GroupId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}