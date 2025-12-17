using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.IEventHandlerServices;
using VideoConferencingApp.Application.Common.INotificationServices;
using VideoConferencingApp.Application.DTOs.Notification;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Events.Notification;
using VideoConferencingApp.Domain.Exceptions;

namespace VideoConferencingApp.Application.EventHandlers.Notifications
{
    public class FirebasePushNotificationHandler : IEventHandler<SendFirebasePushNotificationEvent>
    {
        private readonly IFirebasePushNotificationService _notificationService;
        private readonly IEventValidator _validator;
        private readonly ILogger<FirebasePushNotificationHandler> _logger;

        public FirebasePushNotificationHandler(
            IFirebasePushNotificationService notificationService,
            IEventValidator validator,
            ILogger<FirebasePushNotificationHandler> logger)
        {
            _notificationService = notificationService;
            _validator = validator;
            _logger = logger;
        }

        public async Task HandleAsync(SendFirebasePushNotificationEvent @event)
        {
            try
            {
                _logger.LogInformation(
                    "Processing Firebase push notification event. EventId: {EventId}, Type: {Type}",
                    @event.EventId,
                    @event.Type);

                // Validate the event
                var validationResult = _validator.Validate(@event);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning(
                        "Validation failed for event {EventId}. Errors: {Errors}",
                        @event.EventId,
                        string.Join("; ", validationResult.Errors));
                }

                // Process based on type
                switch (@event.Type)
                {
                    case PushNotificationType.SingleDevice:
                        await HandleSingleDeviceNotification(@event);
                        break;

                    case PushNotificationType.MultipleDevices:
                        await HandleMultipleDevicesNotification(@event);
                        break;

                    case PushNotificationType.Topic:
                        await HandleTopicNotification(@event);
                        break;

                    case PushNotificationType.Batch:
                        await HandleBatchNotification(@event);
                        break;

                    default:
                        throw new ArgumentException($"Unknown notification type: {@event.Type}");
                }

                _logger.LogInformation(
                    "Successfully processed Firebase push notification event. EventId: {EventId}",
                    @event.EventId);
            }
            catch (ValidationException ex)
            {
                _logger.LogError(
                    "Validation error. EventId: {EventId}, Errors: {Errors}",
                    @event.EventId,
                    string.Join("; ", ex.Errors));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event. EventId: {EventId}", @event.EventId);
                throw;
            }
        }

        private async Task HandleSingleDeviceNotification(SendFirebasePushNotificationEvent @event)
        {
            var request = new SingleDeviceNotificationRequest
            {
                DeviceToken = @event.DeviceToken,
                Title = @event.Title,
                Body = @event.Body,
                ImageUrl = @event.ImageUrl,
                Data = @event.Data,
                Priority = @event.Priority,         
                TimeToLive = DateTime.UtcNow.TimeOfDay
            };

            var result = await _notificationService.SendToDeviceAsync(request);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Single device notification sent. MessageId: {MessageId}",
                    result.MessageId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send single device notification. Error: {Error}",
                    result.Error);
            }
        }

        private async Task HandleMultipleDevicesNotification(SendFirebasePushNotificationEvent @event)
        {
            var request = new MultiDeviceNotificationRequest
            {
                DeviceTokens = @event.DeviceTokens,
                Title = @event.Title,
                Body = @event.Body,
                ImageUrl = @event.ImageUrl,
                Data = @event.Data,
                Priority = @event.Priority,
            
                TimeToLive = DateTime.UtcNow.TimeOfDay
            };

            var result = await _notificationService.SendToMultipleDevicesAsync(request);

            _logger.LogInformation(
                "Multiple devices notification sent. Success: {Success}, Failure: {Failure}",
                result.SuccessCount,
                result.FailureCount);

            if (result.FailedTokens.Any())
            {
                _logger.LogWarning(
                    "Failed tokens: {FailedTokens}",
                    string.Join(", ", result.FailedTokens.Take(10)));
            }
        }

        private async Task HandleTopicNotification(SendFirebasePushNotificationEvent @event)
        {
            var request = new TopicNotificationRequest
            {
                Topic = @event.Topic,
                Title = @event.Title,
                Body = @event.Body,
                ImageUrl = @event.ImageUrl,
                Data = @event.Data,
                Priority = @event.Priority,
                TimeToLive = DateTime.UtcNow.TimeOfDay
            };

            var result = await _notificationService.SendToTopicAsync(request);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Topic notification sent. MessageId: {MessageId}, Topic: {Topic}",
                    result.MessageId,
                    @event.Topic);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send topic notification. Error: {Error}",
                    result.Error);
            }
        }

        private async Task HandleBatchNotification(SendFirebasePushNotificationEvent @event)
        {
            var requests = @event.DeviceTokens.Select(token => new SingleDeviceNotificationRequest
            {
                DeviceToken = token,
                Title = @event.Title,
                Body = @event.Body,
                ImageUrl = @event.ImageUrl,
                Data = @event.Data,
                Priority = @event.Priority,
                TimeToLive = DateTime.UtcNow.TimeOfDay
            }).ToList();

            var result = await _notificationService.SendBatchAsync(requests);

            _logger.LogInformation(
                "Batch notification sent. Success: {Success}, Failure: {Failure}, Total: {Total}",
                result.SuccessCount,
                result.FailureCount,
                result.TotalCount);
        }
    }
}
