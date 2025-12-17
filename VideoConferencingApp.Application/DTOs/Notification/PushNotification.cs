using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.DTOs.Notification
{
    public class PushNotification
    {
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}
