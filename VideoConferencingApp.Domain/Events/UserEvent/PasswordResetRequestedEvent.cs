using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Events.UserEvent
{
    public class PasswordResetRequestedEvent : BaseEvent
    {
        public long UserId { get; set; }
        public string Email { get; set; }
        public string IpAddress { get; set; }
    }
}
