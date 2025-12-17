using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.DTOs.Common;

namespace VideoConferencingApp.Application.Common.IEventHandlerServices
{
    public interface IEventValidator
    {
        ValidationResult Validate<TEvent>(TEvent @event) where TEvent : class;
    }
}
