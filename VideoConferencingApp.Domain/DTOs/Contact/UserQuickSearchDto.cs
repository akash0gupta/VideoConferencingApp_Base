using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.DTOs.Contact
{
    public class UserQuickSearchDto
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string ProfilePictureUrl { get; set; }
        public bool IsOnline { get; set; }
    }
}
