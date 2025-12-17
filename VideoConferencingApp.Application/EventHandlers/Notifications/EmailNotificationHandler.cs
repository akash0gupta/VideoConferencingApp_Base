using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.IEventHandlerServices;
using VideoConferencingApp.Application.Common.INotificationServices;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Events.Notification;

namespace VideoConferencingApp.Application.EventHandlers.Notifications
{
    public class EmailNotificationHandler : IEventHandler<SendEmailNotificationEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailNotificationHandler> _logger;

        public EmailNotificationHandler(IEmailService emailService, ILogger<EmailNotificationHandler> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task HandleAsync(SendEmailNotificationEvent eventData)
        {
            _logger.LogInformation("Processing Email Notification for {To}", eventData.To);

            try
            {
                if (!string.IsNullOrEmpty(eventData.TemplateName))
                {
                    await _emailService.SendTemplatedEmailAsync<SendEmailNotificationEvent>(
                        eventData.To,
                        eventData.Subject,
                        eventData.TemplateName,
                        eventData.TemplateData
                    );
                }
                else
                {
                    await _emailService.SendEmailAsync(
                        eventData.To,
                        eventData.Subject,
                        eventData.Body
                    );
                }

                _logger.LogInformation("Email notification sent successfully to {To}", eventData.To);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification to {To}", eventData.To);
                throw;
            }
        }
    }
}
