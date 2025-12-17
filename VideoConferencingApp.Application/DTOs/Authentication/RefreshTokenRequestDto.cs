using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.DTOs.Authentication
{
    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; }
        public string DeviceToken { get; set; }
        public string ?IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
