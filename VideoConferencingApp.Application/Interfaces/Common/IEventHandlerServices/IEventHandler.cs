using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Events;

namespace VideoConferencingApp.Application.Interfaces.Common.IEventHandlerServices
{
    /// <summary>
    /// Defines the contract for a class that can handle a specific type of event.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to handle.</typeparam>
    public interface IEventHandler<in TEvent> where TEvent : BaseEvent
    {
        Task HandleAsync(TEvent @event);
    }
}
