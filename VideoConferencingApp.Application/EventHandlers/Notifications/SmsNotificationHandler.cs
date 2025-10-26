using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.IEventHandlerServices;
using VideoConferencingApp.Application.Interfaces.Common.INotificationServices;
using VideoConferencingApp.Domain.Events.Notification;

namespace VideoConferencingApp.Application.EventHandlers.Notifications
{
    public class SmsNotificationHandler : IEventHandler<SendSmsNotificationEvent>
    {
        private readonly ISmsService _smsService;
        private readonly ILogger<SmsNotificationHandler> _logger;

        public SmsNotificationHandler(ISmsService smsService, ILogger<SmsNotificationHandler> logger)
        {
            _smsService = smsService;
            _logger = logger;
        }

        public async Task HandleAsync(SendSmsNotificationEvent eventData)
        {
            _logger.LogInformation("Processing SMS Notification for {PhoneNumber}", eventData.PhoneNumber);

            try
            {
                await _smsService.SendSmsAsync(eventData.PhoneNumber, eventData.SmsBody);
                _logger.LogInformation("SMS notification sent successfully to {PhoneNumber}", eventData.PhoneNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS notification to {PhoneNumber}", eventData.PhoneNumber);
                throw;
            }
        }
    }
}
