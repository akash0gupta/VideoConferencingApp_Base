using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Entities.User;

namespace VideoConferencingApp.Application.Interfaces.Common.IAuthServices
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(long userId);
        Task SaveAsync(RefreshToken token);
        Task RevokeAsync(string token, string ipAddress);
        Task RevokeAllUserTokensAsync(long userId, string ipAddress);
        Task CleanupExpiredTokensAsync();
    }
}
