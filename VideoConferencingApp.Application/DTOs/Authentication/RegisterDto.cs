using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.DTOs.Authentication
{
    public class RegisterDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string DisplayName { get; set; }
        public string? IpAddress { get; set; } = string.Empty;
        public string? UserAgent { get; set; } = string.Empty;
        public bool AcceptTerms { get; set; } = false;
    }

}
