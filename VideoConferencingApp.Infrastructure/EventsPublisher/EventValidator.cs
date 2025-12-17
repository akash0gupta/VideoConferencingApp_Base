using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.IEventHandlerServices;
using VideoConferencingApp.Domain.CustomAttributes;
using VideoConferencingApp.Application.DTOs.Common;

namespace VideoConferencingApp.Infrastructure.EventsPublisher
{
    public class EventValidator : IEventValidator
    {
        public ValidationResult Validate<TEvent>(TEvent @event) where TEvent : class
        {
            var result = new ValidationResult();

            if (@event == null)
            {
                result.AddError("Event cannot be null");
                return result;
            }

            var properties = typeof(TEvent).GetProperties();

            foreach (var property in properties)
            {
                var value = property.GetValue(@event);

                // Check RequiredField attribute
                var requiredAttr = property.GetCustomAttribute<RequiredFieldAttribute>();
                if (requiredAttr != null)
                {
                    if (!IsValueProvided(value))
                    {
                        result.AddError(requiredAttr.ErrorMessage);
                    }
                }

                // Check RequiredWhen attribute
                var requiredWhenAttr = property.GetCustomAttribute<RequiredWhenAttribute>();
                if (requiredWhenAttr != null)
                {
                    var dependentProp = properties.FirstOrDefault(p =>
                        p.Name == requiredWhenAttr.DependentProperty);

                    if (dependentProp != null)
                    {
                        var dependentValue = dependentProp.GetValue(@event);

                        if (dependentValue?.Equals(requiredWhenAttr.DependentValue) == true)
                        {
                            if (!IsValueProvided(value))
                            {
                                result.AddError(requiredWhenAttr.ErrorMessage);
                            }
                        }
                    }
                }

                // Check MinLength attribute
                var minLengthAttr = property.GetCustomAttribute<MinLengthFieldAttribute>();
                if (minLengthAttr != null && value != null)
                {
                    if (value is string strValue && strValue.Length < minLengthAttr.MinLength)
                    {
                        result.AddError(minLengthAttr.ErrorMessage);
                    }
                    else if (value is ICollection collection && collection.Count < minLengthAttr.MinLength)
                    {
                        result.AddError(minLengthAttr.ErrorMessage);
                    }
                }

                // Check MaxLength attribute
                var maxLengthAttr = property.GetCustomAttribute<MaxLengthFieldAttribute>();
                if (maxLengthAttr != null && value != null)
                {
                    if (value is string strValue && strValue.Length > maxLengthAttr.MaxLength)
                    {
                        result.AddError(maxLengthAttr.ErrorMessage);
                    }
                    else if (value is ICollection collection && collection.Count > maxLengthAttr.MaxLength)
                    {
                        result.AddError(maxLengthAttr.ErrorMessage);
                    }
                }
            }

            return result;
        }

        private bool IsValueProvided(object value)
        {
            if (value == null)
                return false;

            if (value is string strValue)
                return !string.IsNullOrWhiteSpace(strValue);

            if (value is ICollection collection)
                return collection.Count > 0;

            return true;
        }
    }
}
