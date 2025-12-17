namespace VideoConferencingApp.Api.DTOs.ContactDto
{
    public class SendContactRequestDto
    {
        public long AddresseeId { get; set; }
        public string  Message { get; set; } = string.Empty;
    }
}
