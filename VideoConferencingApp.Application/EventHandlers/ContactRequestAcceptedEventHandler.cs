using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.IEventHandlerServices;
using VideoConferencingApp.Application.Events;
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
            

            // Publish Email Notification Event
            await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
            {
                EventId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = eventData.Contact.RequesterId,
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
                    UserId = eventData.Contact.RequesterId,
                    PhoneNumber = eventData.RequesterPhoneNumber,
                    SmsBody = $"Your contact request to {eventData.AddresseeName} has been accepted!"
                });
            }
        }
    }

}
