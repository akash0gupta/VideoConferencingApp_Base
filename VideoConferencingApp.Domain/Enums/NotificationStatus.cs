using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Enums
{
    public enum NotificationStatus
    {
        Pending,
        Queued,
        Sending,
        Sent,
        Delivered,
        Read,
        Failed,
        Expired,
        Cancelled
    }
}
