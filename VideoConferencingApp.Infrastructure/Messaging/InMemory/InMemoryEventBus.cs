using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Application.Common.IEventHandlerServices;
using VideoConferencingApp.Application.Events;
using VideoConferencingApp.Domain.Events;

namespace VideoConferencingApp.Infrastructure.Messaging.InMemory
{
    public class InMemoryEventBus : IMessageProducer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InMemoryEventBus> _logger;
        private readonly Dictionary<Type, List<Type>> _handlers = new();

        public InMemoryEventBus(IServiceProvider serviceProvider, ILogger<InMemoryEventBus> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : BaseEvent
        {
            var eventType = @event.GetType();
            if (!_handlers.ContainsKey(eventType)) return;

            using var scope = _serviceProvider.CreateScope();
            var eventHandlers = _handlers[eventType];

            foreach (var handlerType in eventHandlers)
            {
                var handler = scope.ServiceProvider.GetService(handlerType) as IEventHandler<TEvent>;
                if (handler != null)
                {
                    await handler.HandleAsync(@event);
                }
            }
        }

        public void Subscribe<TEvent, TEventHandler>()
            where TEvent : BaseEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            var eventType = typeof(TEvent);
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers.Add(eventType, new List<Type>());
            }
            _handlers[eventType].Add(typeof(TEventHandler));
        }
    }
}