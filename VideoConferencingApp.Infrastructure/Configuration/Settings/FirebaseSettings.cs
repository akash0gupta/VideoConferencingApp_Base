using FirebaseAdmin.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Application.DTOs.Notification;

namespace VideoConferencingApp.Infrastructure.Configuration.Settings
{
    public class FirebaseSettings:IConfig
    {
        public string SectionName => "FirebaseSettings";
        public string ProjectId { get; set; }
        public string PrivateKey { get; set; }
        public string ClientEmail { get; set; }
        public string JsonFilePath { get; set; } // Optional: path to service account JSON
        public FirebaseNotificationDefaultConfig DefaultConfig { get; set; }
    }

    public class FirebaseNotificationDefaultConfig
    {
        public VideoConferencingApp.Domain.Enums.NotificationPriority Priority { get; set; }

        public TimeSpan TimeToLive { get; set; }

        public AndroidNotificationConfig Android { get; set; }

        public AppleNotificationConfig Apple { get; set; }

        public WebNotificationConfig Web { get; set; }
    }
}
