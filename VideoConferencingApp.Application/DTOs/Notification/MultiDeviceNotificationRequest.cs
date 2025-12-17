namespace VideoConferencingApp.Application.DTOs.Notification
{
    public class MultiDeviceNotificationRequest : PushNotificationRequest
    {
        public List<string> DeviceTokens { get; set; }
    }


   
}
