using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.DTOs.Authentication
{
    public class LogoutDto
    {
        public string RefreshToken { get; set; }
        public string SessionId { get; set; }
        public string DeviceToken { get; set; }
        public bool LogoutFromAllDevices { get; set; }
    }
}
