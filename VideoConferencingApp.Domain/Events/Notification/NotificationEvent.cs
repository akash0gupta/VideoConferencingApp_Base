using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Events.Notification
{
    public class NotificationEvent:BaseEvent
    {
        public NotificationType Type { get; set; }
        public string UserId { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }
}
