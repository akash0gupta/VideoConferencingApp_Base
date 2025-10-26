using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.DTOs.Notification
{
    public class EmailMessage
    {
        public List<string> To { get; set; } = new List<string>();
        public List<string> Cc { get; set; } = new List<string>();
        public List<string> Bcc { get; set; } = new List<string>();
        public List<string> ReplyTo { get; set; } = new List<string>();
        public string Subject { get; set; }
        public string Body { get; set; }
        public string PlainTextBody { get; set; }
        public bool IsHtml { get; set; } = true;
        public NotificationPriority ? Priority { get; set; }
        public List<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
        public List<EmailAttachment> LinkedResources { get; set; } = new List<EmailAttachment>();
    }

}
