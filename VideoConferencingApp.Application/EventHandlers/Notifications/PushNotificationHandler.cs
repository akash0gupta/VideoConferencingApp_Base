using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.IEventHandlerServices;
using VideoConferencingApp.Application.Interfaces.Common.INotificationServices;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Events.Notification;

namespace VideoConferencingApp.Application.EventHandlers.Notifications
{
    public class PushNotificationHandler : IEventHandler<SendPushNotificationEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<PushNotificationHandler> _logger;

        public PushNotificationHandler(INotificationService notificationService, ILogger<PushNotificationHandler> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task HandleAsync(SendPushNotificationEvent eventData)
        {
            _logger.LogInformation("Processing Push Notification for {Target}:{TargetId}",
                eventData.Target, eventData.TargetId);

            try
            {
                switch (eventData.Target)
                {
                    case NotificationTarget.User:
                        await _notificationService.NotifyUserAsync(
                            eventData.TargetId,
                            eventData.Method,
                            eventData.Payload
                        );
                        break;

                    case NotificationTarget.Group:
                        await _notificationService.NotifyGroupAsync(
                            eventData.TargetId,
                            eventData.Method,
                            eventData.Payload
                        );
                        break;

                    case NotificationTarget.All:
                        await _notificationService.NotifyAllAsync(
                            eventData.Method,
                            eventData.Payload
                        );
                        break;
                }

                _logger.LogInformation("Push notification sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send push notification");
                throw;
            }
        }
    }
}
