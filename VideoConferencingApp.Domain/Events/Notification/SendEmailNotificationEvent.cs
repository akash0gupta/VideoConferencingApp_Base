using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Events.Notification
{
    public class SendEmailNotificationEvent : BaseEvent
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string TemplateName { get; set; }
        public long UserId { get; set; }
        public Dictionary<string, string> TemplateData { get; set; }
    }
}
