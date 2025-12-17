using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Entities.UserEntities
{
    public class UserSession : BaseEntity
    {
        public string SessionId { get; set; }
        public long UserId { get; set; }
        public string RefreshToken { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string DeviceName { get; set; }
        public string DeviceType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string Location { get; set; }
    }
}
