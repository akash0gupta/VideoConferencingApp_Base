using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.DTOs.Notification
{
    public class EmailNotification
    {
        public string To { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsHtml { get; set; }
        public List<string>? Cc { get; set; }
        public List<string>? Bcc { get; set; }
        public Dictionary<string, byte[]>? Attachments { get; set; }
    }
}
