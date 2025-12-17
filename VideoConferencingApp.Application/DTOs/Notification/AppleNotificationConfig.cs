namespace VideoConferencingApp.Application.DTOs.Notification
{
    public class AppleNotificationConfig
    {
        public string Sound { get; set; } = "default";
        public int? Badge { get; set; }
        public bool? ContentAvailable { get; set; }
        public bool? MutableContent { get; set; }
        public string Category { get; set; }
        public string ThreadId { get; set; }
        public Dictionary<string, object> CustomData { get; set; }
    }


   
}
