using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.Common.ICommonServices
{
    public interface IRateLimitService
    {
        Task<bool> IsAllowedAsync(string userId, string action);
        Task RecordAttemptAsync(string userId, string action);
        Task<bool> IsBannedAsync(string userId);
        Task BanUserAsync(string userId, TimeSpan duration);
        Task UnbanUserAsync(string userId);
    }
}
