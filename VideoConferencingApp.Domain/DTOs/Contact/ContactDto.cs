using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.DTOs.Contact
{
    public class ContactDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Bio { get; set; }
        public ContactStatus Status { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
        public DateTime ConnectedAt { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsBlocked { get; set; }
    }

}
