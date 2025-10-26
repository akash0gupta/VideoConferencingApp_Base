namespace VideoConferencingApp.Api.Models
{
    public record LoginRequest
    {
        public string UsernameOrEmail { get; set; }
        public string Password { get; set; }
    }
}
