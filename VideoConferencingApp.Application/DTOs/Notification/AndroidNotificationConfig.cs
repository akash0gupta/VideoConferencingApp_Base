using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Notification
{
    // Platform Specific Configuration
    public class AndroidNotificationConfig
    {
        public string ChannelId { get; set; } = "default";
        public string Sound { get; set; } = "default";
        public string Icon { get; set; }
        public string Color { get; set; }
        public NotificationPriority Priority { get; set; } = NotificationPriority.High;
        public string ClickAction { get; set; }
        public int? NotificationCount { get; set; }
        public bool? Sticky { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }


   
}
