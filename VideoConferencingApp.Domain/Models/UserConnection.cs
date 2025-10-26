using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Models
{
    public class UserConnection
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
        public bool IsInCall { get; set; }
        public string? CurrentCallWithUserId { get; set; }
    }
}
