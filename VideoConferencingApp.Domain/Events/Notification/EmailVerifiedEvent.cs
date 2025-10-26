using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Events.Notification
{
    public class EmailVerifiedEvent : BaseEvent
    {
        public long UserId { get; set; }
        public string Email { get; set; }
    }
}
