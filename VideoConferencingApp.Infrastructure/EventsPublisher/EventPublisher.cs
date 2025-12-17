using Microsoft.Extensions.Logging;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Application.Common.IEventHandlerServices;
using VideoConferencingApp.Application.Events;
using VideoConferencingApp.Domain.Events;

namespace VideoConferencingApp.Infrastructure.EventsPublisher
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IMessageProducer _eventBus; 
        private readonly ILogger<EventPublisher> _logger;

        public EventPublisher(
            IMessageProducer eventBus,
            ILogger<EventPublisher> logger)
        {
            _eventBus = eventBus;
            //_notificationService = notificationService;
            _logger = logger;
        }

        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : BaseEvent
        {

            var eventName = @event.GetType().Name;
            _logger.LogInformation("Publishing event through EventPublisher: {EventName}", eventName);
            await _eventBus.PublishAsync(@event);
        }
    }
}