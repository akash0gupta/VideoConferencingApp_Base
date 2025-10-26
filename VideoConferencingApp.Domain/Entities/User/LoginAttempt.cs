using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Entities.User
{
    public class LoginAttempt : BaseEntity
    {
        public string UsernameOrEmail { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public bool IsSuccessful { get; set; }
        public string FailureReason { get; set; }
        public DateTime AttemptedAt { get; set; }
        public long? UserId { get; set; }
        public string Location { get; set; } // Geo-location if available

    }
}
