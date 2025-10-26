using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.DTOs.Authentication
{
    public class PasswordResetRequestDto
    {
        public string Email { get; set; }
        public string IpAddress { get; set; }
    }

}
