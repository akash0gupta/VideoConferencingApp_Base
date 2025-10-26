using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Enums
{
    public enum NotificationType
    {
        System,
        ContactRequest,
        ContactAccepted,
        Message,
        MeetingInvite,
        MeetingReminder,
        MeetingStarted,
        MeetingEnded,
        MissedCall,
        SecurityAlert,
        AccountUpdate,
        Payment,
        Announcement,
        Marketing,
        Custom
    }
}
