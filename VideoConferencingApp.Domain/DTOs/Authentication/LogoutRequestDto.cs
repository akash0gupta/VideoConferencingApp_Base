using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.DTOs.Authentication
{
    public class LogoutRequestDto
    {
        public string RefreshToken { get; set; }
        public string SessionId { get; set; }
        public bool LogoutFromAllDevices { get; set; }
    }
}
