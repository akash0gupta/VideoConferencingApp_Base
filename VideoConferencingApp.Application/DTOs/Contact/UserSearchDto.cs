using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Contact
{
    public class UserSearchDto
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string Bio { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
        public ContactStatus? ContactStatus { get; set; }
        public bool IsSentByCurrentUser { get; set; }
    }
}
