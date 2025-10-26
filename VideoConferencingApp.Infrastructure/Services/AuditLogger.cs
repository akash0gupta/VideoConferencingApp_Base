using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Infrastructure.Services
{
    public class AuditLogger : IAuditLogger
    {
        private readonly ConcurrentBag<AuditLog> _auditLogs = new();
        private readonly ILogger<AuditLogger> _logger;

        public AuditLogger(ILogger<AuditLogger> logger)
        {
            _logger = logger;
        }

        public Task LogAsync(AuditLog auditLog)
        {
            _auditLogs.Add(auditLog);

            var logLevel = auditLog.Severity switch
            {
                AuditSeverity.Critical => LogLevel.Critical,
                AuditSeverity.Warning => LogLevel.Warning,
                _ => LogLevel.Information
            };

            _logger.Log(
                logLevel,
                "AUDIT: User={UserId}, Action={Action}, Target={TargetUserId}, IP={IpAddress}, Details={Details}",
                auditLog.UserId,
                auditLog.Action,
                auditLog.TargetUserId ?? "N/A",
                auditLog.IpAddress,
                auditLog.Details
            );

            return Task.CompletedTask;
        }

        public Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, DateTime? from = null, DateTime? to = null)
        {
            var logs = _auditLogs
                .Where(l => l.UserId == userId)
                .Where(l => !from.HasValue || l.Timestamp >= from.Value)
                .Where(l => !to.HasValue || l.Timestamp <= to.Value)
                .OrderByDescending(l => l.Timestamp)
                .ToList();

            return Task.FromResult(logs);
        }

        public Task<List<AuditLog>> GetAuditLogsByActionAsync(string action, DateTime? from = null, DateTime? to = null)
        {
            var logs = _auditLogs
                .Where(l => l.Action == action)
                .Where(l => !from.HasValue || l.Timestamp >= from.Value)
                .Where(l => !to.HasValue || l.Timestamp <= to.Value)
                .OrderByDescending(l => l.Timestamp)
                .ToList();

            return Task.FromResult(logs);
        }

        public Task<List<AuditLog>> GetCriticalAuditLogsAsync(DateTime? from = null, DateTime? to = null)
        {
            var logs = _auditLogs
                .Where(l => l.Severity == AuditSeverity.Critical)
                .Where(l => !from.HasValue || l.Timestamp >= from.Value)
                .Where(l => !to.HasValue || l.Timestamp <= to.Value)
                .OrderByDescending(l => l.Timestamp)
                .ToList();

            return Task.FromResult(logs);
        }
    }
}
