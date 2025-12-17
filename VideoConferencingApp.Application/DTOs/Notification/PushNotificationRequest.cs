using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Notification
{
    // Complete Notification Request
    public class PushNotificationRequest
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public string ImageUrl { get; set; }
        public Dictionary<string, string> Data { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.High;
        public TimeSpan? TimeToLive { get; set; }
    }


   
}
