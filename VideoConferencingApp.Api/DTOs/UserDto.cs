namespace VideoConferencingApp.Api.Models
{
    public class UserDto
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Bio { get; set; }
        public string Role { get; set; }
        public bool EmailVerified { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
