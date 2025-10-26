using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Events.UserEvent
{
    public class UserLoggedInEvent : BaseEvent
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string SessionId { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
    }
}
