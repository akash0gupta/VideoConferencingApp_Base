using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Events;

namespace VideoConferencingApp.Application.Interfaces.Common.IEventHandlerServices
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T @event) where T : BaseEvent;
    }
}
