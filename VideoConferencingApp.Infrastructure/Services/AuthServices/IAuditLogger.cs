using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Infrastructure.Services.AuthServices
{
    public interface IAuditLogger
    {
        Task LogAsync(AuditLog auditLog);
        Task<List<AuditLog>> GetUserAuditLogsAsync(string userId, DateTime? from = null, DateTime? to = null);
        Task<List<AuditLog>> GetAuditLogsByActionAsync(string action, DateTime? from = null, DateTime? to = null);
        Task<List<AuditLog>> GetCriticalAuditLogsAsync(DateTime? from = null, DateTime? to = null);
    }
}
