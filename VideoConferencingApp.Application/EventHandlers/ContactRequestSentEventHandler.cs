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
    public class ContactRequestSentEventHandler : IEventHandler<ContactRequestSentEvent>
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<ContactRequestSentEventHandler> _logger;

        public ContactRequestSentEventHandler(
            IEventPublisher eventPublisher,
            ILogger<ContactRequestSentEventHandler> logger)
        {
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task HandleAsync(ContactRequestSentEvent eventData)
        {
            _logger.LogInformation("ContactRequestSent Event Handler Call");
            if (eventData.Contact == null)
                return;
            // Notify addressee via Push
            await _eventPublisher.PublishAsync(new SendPushNotificationEvent
            {
                EventId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = eventData.Contact.AddresseeId.ToString(),
                Target = NotificationTarget.User,
                TargetId = eventData.Contact.AddresseeId.ToString(),
                Method = "ContactRequestReceived",
                Payload = new
                {
                    ContactId = eventData.Contact.Id,
                    RequesterId = eventData.Contact.RequesterId,
                    RequesterName = eventData.RequesterName
                }
            });

            // Send Email to Addressee
            await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
            {
                EventId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = eventData.Contact.AddresseeId.ToString(),
                To = eventData.AddresseeEmail,
                Subject = "New Contact Request",
                TemplateName = "ContactRequestReceived",
                TemplateData = new Dictionary<string, string>
                {
                    { "AddresseeName", eventData.AddresseeName },
                    { "RequesterName", eventData.RequesterName },
                    { "RequestDate", DateTime.UtcNow.ToString("yyyy-MM-dd") }
                }
            });
        }
    }


}
