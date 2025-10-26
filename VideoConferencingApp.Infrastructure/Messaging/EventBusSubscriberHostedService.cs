using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VideoConferencingApp.Application.EventHandlers;
using VideoConferencingApp.Application.EventHandlers.Notifications;
using VideoConferencingApp.Application.Events;
using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;
using VideoConferencingApp.Domain.Events.ContactEvents;
using VideoConferencingApp.Domain.Events.Notification;


namespace VideoConferencingApp.Infrastructure.Messaging
{
    public class EventBusSubscriberHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventBusSubscriberHostedService> _logger;

        public EventBusSubscriberHostedService(IServiceProvider serviceProvider, ILogger<EventBusSubscriberHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Event Bus Subscriber Service is starting.");

            using (var scope = _serviceProvider.CreateScope())
            {
                var messageBus = scope.ServiceProvider.GetRequiredService<IMessageProducer>();

                // Domain Event Subscriptions
                messageBus.Subscribe<ContactRequestAcceptedEvent, ContactRequestAcceptedEventHandler>();
                messageBus.Subscribe<ContactRequestSentEvent, ContactRequestSentEventHandler>();

                // Notification Event Subscriptions
                messageBus.Subscribe<SendEmailNotificationEvent, EmailNotificationHandler>();
                messageBus.Subscribe<SendSmsNotificationEvent, SmsNotificationHandler>();
                messageBus.Subscribe<SendPushNotificationEvent, PushNotificationHandler>();
            }

            _logger.LogInformation("Event Bus Subscriber Service has started and subscriptions are configured.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Event Bus Subscriber Service is stopping.");
            return Task.CompletedTask;
        }
    }
}