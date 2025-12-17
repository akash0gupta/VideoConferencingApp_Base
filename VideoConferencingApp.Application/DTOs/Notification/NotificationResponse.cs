namespace VideoConferencingApp.Application.DTOs.Notification
{
    // Response Models
    public class NotificationResponse
    {
        public bool Success { get; set; }
        public string MessageId { get; set; }
        public string Error { get; set; }
        public string ErrorCode { get; set; }
    }
   
}
