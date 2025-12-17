using System;
using VideoConferencingApp.Application.Common.IEventHandlerServices;
using VideoConferencingApp.Domain.Events;

namespace VideoConferencingApp.Application.Common.ICommonServices
{
    /// <summary>
    /// Defines a high-level contract for a message bus that supports the Publish/Subscribe pattern.
    /// </summary>
    public interface IMessageProducer
    {
        /// <summary>
        /// Publishes an event to the message bus.
        /// </summary>
        Task PublishAsync<TEvent>(TEvent @event) where TEvent : BaseEvent;

        /// <summary>
        /// Subscribes an event handler to listen for a specific type of event.
        /// </summary>
        void Subscribe<TEvent, TEventHandler>()
            where TEvent : BaseEvent
            where TEventHandler :IEventHandler<TEvent>;
    }
}