using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.DTOs.Notification
{
    public class EmailTemplateMessage<T> : EmailMessage
    {
        public string TemplateName { get; set; }
        public Dictionary<string, string> TemplateData { get; set; } = new Dictionary<string, string>();
        public List<T> TableData { get; set; }
    }

}
