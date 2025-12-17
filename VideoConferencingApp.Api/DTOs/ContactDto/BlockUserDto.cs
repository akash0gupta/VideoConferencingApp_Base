namespace VideoConferencingApp.Api.DTOs.ContactDto
{
    public class BlockUserDto
    {
        public long UserToBlockId { get; set; }
        public string? Reason { get; set; }
    }
}
