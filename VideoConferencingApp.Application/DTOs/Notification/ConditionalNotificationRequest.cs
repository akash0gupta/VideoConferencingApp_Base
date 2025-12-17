namespace VideoConferencingApp.Application.DTOs.Notification
{
    public class ConditionalNotificationRequest : PushNotificationRequest
    {
        public string Condition { get; set; }
    }


   
}
