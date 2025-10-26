using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.DTOs.Authentication
{
    public class LoginRequestDto
    {
        public string UsernameOrEmail { get; set; }
        public string Password { get; set; }
        public string TwoFactorCode { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public bool RememberMe { get; set; }
    }
}
