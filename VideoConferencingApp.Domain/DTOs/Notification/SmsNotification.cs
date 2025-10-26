using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.DTOs.Notification
{
    public class SmsNotification
    {
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
    }

}
