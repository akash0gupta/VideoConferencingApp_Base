using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Exceptions
{
    public class TooManyRequestsException : Exception
    {
        public int RetryAfterSeconds { get; }

        public TooManyRequestsException(string message, int retryAfterSeconds = 60) : base(message)
        {
            RetryAfterSeconds = retryAfterSeconds;
        }
    }
}
