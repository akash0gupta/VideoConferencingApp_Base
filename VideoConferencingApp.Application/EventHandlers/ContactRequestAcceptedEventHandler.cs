using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Events;
using VideoConferencingApp.Application.Interfaces.Common.IEventHandlerServices;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Events.ContactEvents;
using VideoConferencingApp.Domain.Events.Notification;

namespace VideoConferencingApp.Application.EventHandlers
{
    public class ContactRequestAcceptedEventHandler : IEventHandler<ContactRequestAcceptedEvent>
    {
        private readonly IEventPublisher _eventPublisher; // Changed from INotificationService
        private readonly ILogger<ContactRequestAcceptedEventHandler> _logger;

        public ContactRequestAcceptedEventHandler(
            IEventPublisher eventPublisher,
            ILogger<ContactRequestAcceptedEventHandler> logger)
        {
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task HandleAsync(ContactRequestAcceptedEvent eventData)
        {
            _logger.LogInformation("ContactRequestAccepted Event Handler Call");
            if (eventData.Contact == null)
                return;
            // Publish Push Notification Event
            await _eventPublisher.PublishAsync(new SendPushNotificationEvent
            {
                EventId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = eventData.Contact.RequesterId.ToString(),
                Target = NotificationTarget.User,
                TargetId = eventData.Contact.RequesterId.ToString(),
                Method = "ContactRequestAccepted",
                Payload = new
                {
                    ContactId = eventData.Contact.Id,
                    AddresseeId = eventData.Contact.AddresseeId
                }
            });

            // Publish Email Notification Event
            await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
            {
                EventId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = eventData.Contact.RequesterId.ToString(),
                To = eventData.RequesterEmail, // You need to pass this in the event
                Subject = "Contact Request Accepted",
                TemplateName = "ContactRequestAccepted",
                TemplateData = new Dictionary<string, string>
                {
                    { "RequesterName", eventData.RequesterName },
                    { "AddresseeName", eventData.AddresseeName },
                    { "AcceptedDate", DateTime.UtcNow.ToString("yyyy-MM-dd") }
                }
            });

            // Optionally publish SMS Notification Event
            if (!string.IsNullOrEmpty(eventData.RequesterPhoneNumber))
            {
                await _eventPublisher.PublishAsync(new SendSmsNotificationEvent
                {
                    EventId = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    UserId = eventData.Contact.RequesterId.ToString(),
                    PhoneNumber = eventData.RequesterPhoneNumber,
                    SmsBody = $"Your contact request to {eventData.AddresseeName} has been accepted!"
                });
            }
        }
    }

}
