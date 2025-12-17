using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.CustomAttributes;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Events.Notification
{

        public class SendFirebasePushNotificationEvent : BaseEvent
        {
            [RequiredWhen("DeviceToken", nameof(Type), PushNotificationType.SingleDevice)]
            public string DeviceToken { get; set; }

            [RequiredWhen("DeviceTokens", nameof(Type), PushNotificationType.MultipleDevices)]
            [MinLengthField("DeviceTokens", 1)]
            [MaxLengthField("DeviceTokens", 500)]
            public List<string> DeviceTokens { get; set; }

            [RequiredWhen("Topic", nameof(Type), PushNotificationType.Topic)]
            [MinLengthField("Topic", 1)]
            [MaxLengthField("Topic", 100)]
            public string Topic { get; set; }

            [RequiredField("Title")]
            [MinLengthField("Title", 1)]
            [MaxLengthField("Title", 100)]
            public string Title { get; set; }

            [RequiredField("Body")]
            [MinLengthField("Body", 1)]
            [MaxLengthField("Body", 500)]
            public string Body { get; set; }

            public Dictionary<string, string> Data { get; set; }

            [RequiredField("PushNotificationType")]
            public PushNotificationType Type { get; set; }

            public NotificationPriority Priority { get; set; } = NotificationPriority.High;

            [MaxLengthField("ImageUrl", 500)]
            public string ImageUrl { get; set; }

            public int? Badge { get; set; }

            [MaxLengthField("Sound", 50)]
            public string Sound { get; set; } = "default";
        }
    
    
}
