namespace VideoConferencingApp.Api.Models
{
    public class VerifyTwoFactorDto
    {
        public string Code { get; set; }
        public string SessionToken { get; set; }
    }
}
