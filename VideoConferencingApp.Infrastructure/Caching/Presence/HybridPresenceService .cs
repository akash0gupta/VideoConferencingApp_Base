using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.IRealTimeServices;
using VideoConferencingApp.Infrastructure.Configuration.Redis;
using VideoConferencingApp.Infrastructure.Configuration.Settings;

namespace VideoConferencingApp.Infrastructure.Caching.Presence
{
    public class HybridPresenceService : IPresenceService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly RedisPresenceService _redisService;
        private readonly InMemoryPresenceService _inMemoryService;
        private readonly ILogger<HybridPresenceService> _logger;

        public HybridPresenceService(
            RedisConnectionManager connectionManager,
            ILogger<HybridPresenceService> logger,
            ILogger<RedisPresenceService> redisLogger)
        {
            _connectionMultiplexer = connectionManager.GetRedisConnection(CacheSettings.PresenceConnectionKey) ?? throw new ArgumentNullException("Connection Not Satblish Redis");
            _logger = logger;
            _redisService = new RedisPresenceService(connectionManager, redisLogger);
            _inMemoryService = new InMemoryPresenceService();
        }

        private bool IsRedisAvailable => _connectionMultiplexer.IsConnected;

        public async Task UserConnectedAsync(long userId, string connectionId)
        {
            if (IsRedisAvailable)
            {
                try
                {
                    await _redisService.UserConnectedAsync(userId, connectionId);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis operation failed, falling back to in-memory");
                }
            }

            await _inMemoryService.UserConnectedAsync(userId, connectionId);
        }

        public async Task UserDisconnectedAsync(long userId, string connectionId)
        {
            if (IsRedisAvailable)
            {
                try
                {
                    await _redisService.UserDisconnectedAsync(userId, connectionId);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis operation failed, falling back to in-memory");
                }
            }

            await _inMemoryService.UserDisconnectedAsync(userId, connectionId);
        }

        public async Task<bool> IsOnlineAsync(long userId)
        {
            if (IsRedisAvailable)
            {
                try
                {
                    return await _redisService.IsOnlineAsync(userId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis operation failed, falling back to in-memory");
                }
            }

            return await _inMemoryService.IsOnlineAsync(userId);
        }

        public async Task<long[]> GetOnlineUserIdsAsync()
        {
            if (IsRedisAvailable)
            {
                try
                {
                    return await _redisService.GetOnlineUserIdsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis operation failed, falling back to in-memory");
                }
            }

            return await _inMemoryService.GetOnlineUserIdsAsync();
        }
    }
}
