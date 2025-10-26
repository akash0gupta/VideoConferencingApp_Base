using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.IRealTimeServices;
using VideoConferencingApp.Infrastructure.Configuration;
using VideoConferencingApp.Infrastructure.Configuration.Settings;

namespace VideoConferencingApp.Infrastructure.Services
{
    public class RateLimitService : IRateLimitService
    {
        private readonly IMemoryCache _cache;
        private readonly RateLimitingSettings _settings;
        private readonly ILogger<RateLimitService> _logger;

        public RateLimitService(
            IMemoryCache cache,
            AppSettings settings,
            ILogger<RateLimitService> logger)
        {
            _cache = cache;
            _settings = settings.Get<RateLimitingSettings>();
            _logger = logger;
        }

        public Task<bool> IsAllowedAsync(string userId, string action)
        {
            var key = GetCacheKey(userId, action);

            if (!_cache.TryGetValue(key, out int attemptCount))
            {
                return Task.FromResult(true);
            }

            var limit = GetLimitForAction(action);
            var isAllowed = attemptCount < limit;

            if (!isAllowed)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for user {UserId}, action {Action}: {Count}/{Limit}",
                    userId, action, attemptCount, limit
                );
            }

            return Task.FromResult(isAllowed);
        }

        public Task RecordAttemptAsync(string userId, string action)
        {
            var key = GetCacheKey(userId, action);
            var duration = GetDurationForAction(action);

            var attemptCount = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = duration;
                return 0;
            });

            _cache.Set(key, attemptCount + 1, duration);

            return Task.CompletedTask;
        }

        public Task<bool> IsBannedAsync(string userId)
        {
            var key = GetBanKey(userId);
            var isBanned = _cache.TryGetValue(key, out _);
            return Task.FromResult(isBanned);
        }

        public Task BanUserAsync(string userId, TimeSpan duration)
        {
            var key = GetBanKey(userId);
            _cache.Set(key, true, duration);

            _logger.LogWarning("User {UserId} has been banned for {Duration}", userId, duration);

            return Task.CompletedTask;
        }

        public Task UnbanUserAsync(string userId)
        {
            var key = GetBanKey(userId);
            _cache.Remove(key);

            _logger.LogInformation("User {UserId} has been unbanned", userId);

            return Task.CompletedTask;
        }

        private string GetCacheKey(string userId, string action) => $"ratelimit:{userId}:{action}";
        private string GetBanKey(string userId) => $"ban:{userId}";

        private int GetLimitForAction(string action) => action switch
        {
            "call" => _settings.MaxCallAttemptsPerMinute,
            "connect" => _settings.MaxConnectionAttemptsPerHour,
            "ice" => _settings.MaxIceCandidatesPerMinute,
            _ => 100
        };

        private TimeSpan GetDurationForAction(string action) => action switch
        {
            "call" => TimeSpan.FromMinutes(1),
            "connect" => TimeSpan.FromHours(1),
            "ice" => TimeSpan.FromMinutes(1),
            _ => TimeSpan.FromMinutes(1)
        };
    }
}
