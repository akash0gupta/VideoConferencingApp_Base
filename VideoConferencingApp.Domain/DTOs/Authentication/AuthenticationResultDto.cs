using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.DTOs.Authentication
{
    public class AuthenticationResultDto
    {
        public bool Success { get; set; }
        public long UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string SessionId { get; set; }
        public string Role { get; set; }
        public List<string> Permissions { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public bool RequiresEmailVerification { get; set; }
        public bool RequiresPasswordChange { get; set; }
        public string Message { get; set; }
    }

}
