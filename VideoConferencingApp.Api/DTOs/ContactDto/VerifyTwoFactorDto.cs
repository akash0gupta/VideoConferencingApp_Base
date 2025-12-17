namespace VideoConferencingApp.Api.DTOs.ContactDto
{
    public class VerifyTwoFactorDto
    {
        public string Code { get; set; }
        public string SessionToken { get; set; }
    }
}
