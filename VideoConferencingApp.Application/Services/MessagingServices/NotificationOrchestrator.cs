using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using VideoConferencingApp.Application.Common.IEventHandlerServices;
using VideoConferencingApp.Application.Services.UserServices;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Events.Notification;

namespace VideoConferencingApp.Application.Services.MessagingServices
{
    public class NotificationOrchestrator : INotificationOrchestrator
    {
        private readonly IConnectionManagerService _connectionManager;
        private readonly IUserDeviceTokenService _deviceTokenService;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<NotificationOrchestrator> _logger;

        public NotificationOrchestrator(
            IConnectionManagerService connectionManager,
            IUserDeviceTokenService deviceTokenService,
            IEventPublisher eventPublisher,
            ILogger<NotificationOrchestrator> logger)
        {
            _connectionManager = connectionManager;
            _deviceTokenService = deviceTokenService;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task NotifyMessageAsync(
            string receiverId,
            string senderId,
            string senderName,
            string message,
            string messageId)
        {
            try
            {
                // Check if user is connected to SignalR hub
                var isConnected = await _connectionManager.IsUserOnlineAsync(receiverId);

                if (isConnected)
                {
                    // User is connected to hub, notification will be sent via SignalR
                    _logger.LogDebug("User {ReceiverId} is connected to hub, message will be sent via SignalR", receiverId);
                    return;
                }

                // User is offline or not connected to hub - send push notification
                _logger.LogInformation("User {ReceiverId} is offline, sending push notification for message", receiverId);

                await SendPushNotificationAsync(
                    userId: receiverId,
                    title: $"New message from {senderName}",
                    body: TruncateMessage(message, 100),
                    data: new Dictionary<string, string>
                    {
                        { "type", "message" },
                        { "messageId", messageId },
                        { "senderId", senderId },
                        { "senderName", senderName }
                    },
                    priority: NotificationPriority.High
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying message to user {ReceiverId}", receiverId);
            }
        }

        public async Task NotifyIncomingCallAsync(
            string receiverId,
            string callerId,
            string callerName,
            string callerAvatar,
            string callId,
            CallsType callType)
        {
            try
            {
                var isConnected = await _connectionManager.IsUserOnlineAsync(receiverId);

                if (isConnected)
                {
                    // User is connected, SignalR will handle it
                    _logger.LogDebug("User {ReceiverId} is connected, call notification via SignalR", receiverId);
                    return;
                }

                // Send high-priority push notification for incoming call
                _logger.LogInformation("User {ReceiverId} is offline, sending push notification for incoming call", receiverId);

                await SendPushNotificationAsync(
                    userId: receiverId,
                    title: $"Incoming {callType} call",
                    body: $"{callerName} is calling you",
                    imageUrl: callerAvatar,
                    data: new Dictionary<string, string>
                    {
                        { "type", "incoming_call" },
                        { "callId", callId },
                        { "callerId", callerId },
                        { "callerName", callerName },
                        { "callType", callType.ToString() }
                    },
                    priority: NotificationPriority.High,
                    sound: "call_ringtone.wav"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying incoming call to user {ReceiverId}", receiverId);
            }
        }

        public async Task NotifyMissedCallAsync(string userId, string callerId, string callerName, string callId)
        {
            try
            {
                await SendPushNotificationAsync(
                    userId: userId,
                    title: "Missed call",
                    body: $"You missed a call from {callerName}",
                    data: new Dictionary<string, string>
                    {
                        { "type", "missed_call" },
                        { "callId", callId },
                        { "callerId", callerId },
                        { "callerName", callerName }
                    },
                    priority: NotificationPriority.Normal
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying missed call to user {UserId}", userId);
            }
        }

        public async Task NotifyContactRequestAsync(
            string receiverId,
            string requesterId,
            string requesterName,
            string requesterAvatar,
            long contactId)
        {
            try
            {
                var isConnected = await _connectionManager.IsUserOnlineAsync(receiverId);

                if (isConnected)
                {
                    _logger.LogDebug("User {ReceiverId} is connected, contact request via SignalR", receiverId);
                    return;
                }

                await SendPushNotificationAsync(
                    userId: receiverId,
                    title: "New contact request",
                    body: $"{requesterName} wants to connect with you",
                    imageUrl: requesterAvatar,
                    data: new Dictionary<string, string>
                    {
                        { "type", "contact_request" },
                        { "contactId", contactId.ToString() },
                        { "requesterId", requesterId },
                        { "requesterName", requesterName }
                    },
                    priority: NotificationPriority.High
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying contact request to user {ReceiverId}", receiverId);
            }
        }

        public async Task NotifyGroupMessageAsync(
            string groupId,
            List<string> memberIds,
            string senderId,
            string senderName,
            string message)
        {
            try
            {
                foreach (var memberId in memberIds)
                {
                    if (memberId == senderId) continue; // Don't notify sender

                    var isConnected = await _connectionManager.IsUserOnlineAsync(memberId);

                    if (!isConnected)
                    {
                        await SendPushNotificationAsync(
                            userId: memberId,
                            title: $"New message in group",
                            body: $"{senderName}: {TruncateMessage(message, 80)}",
                            data: new Dictionary<string, string>
                            {
                                { "type", "group_message" },
                                { "groupId", groupId },
                                { "senderId", senderId },
                                { "senderName", senderName }
                            },
                            priority: NotificationPriority.Normal
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying group message for group {GroupId}", groupId);
            }
        }

        #region Helper Methods

        private async Task SendPushNotificationAsync(
            string userId,
            string title,
            string body,
            Dictionary<string, string> data,
            NotificationPriority priority,
            string? imageUrl = null,
            string? sound = null)
        {
            try
            {
                // Get user's device tokens
                var devices = await _deviceTokenService.GetUserDevicesAsync(long.Parse(userId), activeOnly: true);

                if (!devices.Any())
                {
                    _logger.LogWarning("User {UserId} has no active devices for push notification", userId);
                    return;
                }

                var deviceTokens = devices.Select(d => d.DeviceToken).ToList();

                // Publish Firebase push notification event
                var pushEvent = new SendFirebasePushNotificationEvent
                {
                    EventId = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    Type = deviceTokens.Count == 1
                        ? PushNotificationType.SingleDevice
                        : PushNotificationType.MultipleDevices,
                    DeviceToken = deviceTokens.Count == 1 ? deviceTokens[0] : null,
                    DeviceTokens = deviceTokens.Count > 1 ? deviceTokens : null,
                    Title = title,
                    Body = body,
                    ImageUrl = imageUrl,
                    Data = data,
                    Priority = priority,
                    Sound = sound,
                    Badge = null // You can implement badge count logic
                };

                await _eventPublisher.PublishAsync(pushEvent);

                _logger.LogInformation(
                    "Push notification event published for user {UserId} to {DeviceCount} device(s)",
                    userId, deviceTokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification to user {UserId}", userId);
                throw;
            }
        }

        private string TruncateMessage(string message, int maxLength)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            if (message.Length <= maxLength)
                return message;

            return message.Substring(0, maxLength - 3) + "...";
        }

        #endregion
    }
}
