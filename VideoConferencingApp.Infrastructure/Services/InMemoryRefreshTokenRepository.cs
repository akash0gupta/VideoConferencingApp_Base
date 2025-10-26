using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.IAuthServices;
using VideoConferencingApp.Domain.Entities.User;

namespace VideoConferencingApp.Infrastructure.Services
{
    public class InMemoryRefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ConcurrentDictionary<string, RefreshToken> _tokens = new();
        private readonly ILogger<InMemoryRefreshTokenRepository> _logger;

        public InMemoryRefreshTokenRepository(ILogger<InMemoryRefreshTokenRepository> logger)
        {
            _logger = logger;
        }

        public Task<RefreshToken?> GetByTokenAsync(string token)
        {
            _tokens.TryGetValue(token, out var refreshToken);
            return Task.FromResult(refreshToken);
        }

        public Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(long userId)
        {
            var tokens = _tokens.Values
                .Where(t => t.UserId == userId && t.IsActive)
                .ToList();

            return Task.FromResult(tokens);
        }

        public Task SaveAsync(RefreshToken token)
        {
            _tokens[token.Token] = token;
            _logger.LogInformation("Refresh token saved for user {UserId}", token.UserId);
            return Task.CompletedTask;
        }

        public Task RevokeAsync(string token, string ipAddress)
        {
            if (_tokens.TryGetValue(token, out var refreshToken))
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.IpAddress = ipAddress;

                _logger.LogInformation("Refresh token revoked for user {UserId}", refreshToken.UserId);
            }

            return Task.CompletedTask;
        }

        public Task RevokeAllUserTokensAsync(long userId, string ipAddress)
        {
            var userTokens = _tokens.Values.Where(t => t.UserId == userId && t.IsActive);

            foreach (var token in userTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.IpAddress = ipAddress;
            }

            _logger.LogInformation("All refresh tokens revoked for user {UserId}", userId);
            return Task.CompletedTask;
        }

        public Task CleanupExpiredTokensAsync()
        {
            var expiredTokens = _tokens.Where(kvp => kvp.Value.IsActive).ToList();

            foreach (var token in expiredTokens)
            {
                _tokens.TryRemove(token.Key, out _);
            }

            _logger.LogInformation("Cleaned up {Count} expired tokens", expiredTokens.Count);
            return Task.CompletedTask;
        }
    }
}
