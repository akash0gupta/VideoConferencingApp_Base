using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Events.Notification
{
    public class SendPushNotificationEvent : NotificationEvent
    {
        public string Method { get; set; }
        public object Payload { get; set; }
        public NotificationTarget Target { get; set; }
        public string TargetId { get; set; }
    }
}
