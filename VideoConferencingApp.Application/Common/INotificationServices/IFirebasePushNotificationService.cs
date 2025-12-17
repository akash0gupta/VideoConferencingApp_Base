using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.DTOs.Notification;

namespace VideoConferencingApp.Application.Common.INotificationServices
{
    /// <summary>
    /// Defines a contract for sending Firebase Cloud Messaging (FCM) push notifications.
    /// </summary>
    public interface IFirebasePushNotificationService
    {
        // Single Device Operations
        Task<NotificationResponse> SendToDeviceAsync(SingleDeviceNotificationRequest request);

        // Multiple Devices Operations
        Task<BatchNotificationResponse> SendToMultipleDevicesAsync(MultiDeviceNotificationRequest request);

        // Topic Operations
        Task<NotificationResponse> SendToTopicAsync(TopicNotificationRequest request);
        Task<NotificationResponse> SendToConditionAsync(ConditionalNotificationRequest request);

        // Topic Management
        Task<TopicManagementResponseDto> SubscribeToTopicAsync(List<string> deviceTokens, string topic);
        Task<TopicManagementResponseDto> UnsubscribeFromTopicAsync(List<string> deviceTokens, string topic);

        // Batch Operations
        Task<BatchNotificationResponse> SendBatchAsync(List<SingleDeviceNotificationRequest> requests);

        // Token Validation
        Task<bool> ValidateDeviceTokenAsync(string deviceToken);
    }
}
