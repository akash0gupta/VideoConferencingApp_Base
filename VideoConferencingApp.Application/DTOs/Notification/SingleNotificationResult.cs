using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Notification
{
    public class SingleNotificationResult
    {
        public bool Success { get; set; }
        public string MessageId { get; set; }
        public string DeviceToken { get; set; }
        public string Error { get; set; }
        public NotificationErrorCode? ErrorCode { get; set; }
    }


   
}
