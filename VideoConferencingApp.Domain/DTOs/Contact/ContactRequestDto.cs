using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.DTOs.Contact
{
    public class ContactRequestDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Message { get; set; }
        public DateTime RequestedAt { get; set; }
        public bool IsReceived { get; set; }
        public ContactStatus Status { get; set; }
    }
}
