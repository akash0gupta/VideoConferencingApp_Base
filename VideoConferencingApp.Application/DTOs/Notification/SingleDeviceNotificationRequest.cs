namespace VideoConferencingApp.Application.DTOs.Notification
{
    public class SingleDeviceNotificationRequest : PushNotificationRequest
    {
        public string DeviceToken { get; set; }
    }
   
}
