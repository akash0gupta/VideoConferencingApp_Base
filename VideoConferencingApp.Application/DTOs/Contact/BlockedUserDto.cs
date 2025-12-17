using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.DTOs.Contact
{
    public class BlockedUserDto
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public DateTime BlockedAt { get; set; }
        public string BlockReason { get; set; }
    }

}
