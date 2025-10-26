using FluentValidation.Results;

namespace VideoConferencingApp.Domain.Exceptions
{
    /// <summary>
    /// Represents an exception that occurs when a command or query fails validation.
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// A dictionary where the key is the property name and the value is an array of error messages for that property.
        /// </summary>
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException(string validation= "One or more validation failures have occurred.")
            : base(validation)
        {
            Errors = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Creates a new ValidationException from a collection of FluentValidation failures.
        /// This is the most common constructor to use.
        /// </summary>
        public ValidationException(IEnumerable<ValidationFailure> failures)
            : this()
        {
            Errors = failures
                .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
        }
    }
}