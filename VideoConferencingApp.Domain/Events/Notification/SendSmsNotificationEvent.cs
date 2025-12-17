using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Events.Notification
{
    public class SendSmsNotificationEvent : BaseEvent
    {
        public string PhoneNumber { get; set; }
        public string SmsBody { get; set; }
        public long UserId { get; set; }
    }
}
