namespace VideoConferencingApp.Api.DTOs
{
    public class SendContactRequestDto
    {
        public long AddresseeId { get; set; }
        public string Message { get; set; }
    }

    public class RejectRequestDto
    {
        public string Reason { get; set; }
    }

    public class BlockUserDto
    {
        public long UserToBlockId { get; set; }
        public string Reason { get; set; }
    }

    public class VerifyTwoFactorDto
    {
        public string Code { get; set; }
        public string SessionToken { get; set; }
    }
}
