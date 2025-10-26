using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.DTOs.Authentication
{
    public class ConfirmTwoFactorDto
    {
        public long UserId { get; set; }
        public string Code { get; set; }
    }
}
