using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Infrastructure.Services
{
    public enum AuditSeverity
    {
        Info,
        Warning,
        Critical
    }

    public class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string? TargetUserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public AuditSeverity Severity { get; set; } = AuditSeverity.Info;
        public string? Details { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
