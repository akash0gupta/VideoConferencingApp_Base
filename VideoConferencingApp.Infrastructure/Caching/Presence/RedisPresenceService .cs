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
    public class RedisPresenceService : IPresenceService
    {
        private readonly IDatabase _redisDatabase;
        private readonly ILogger<RedisPresenceService> _logger;
        private const string PresenceHashKey = "user_presence";
        private const string UserConnectionsKeyPrefix = "user_connections:";

        public RedisPresenceService(
            RedisConnectionManager connectionManager,
            ILogger<RedisPresenceService> logger)
        {
            _redisDatabase = (connectionManager.GetRedisConnection(CacheSettings.PresenceConnectionKey) ?? throw new ArgumentNullException("Connection Not Satblish Redis")).GetDatabase();
            _logger = logger;
        }

        public async Task UserConnectedAsync(long userId, string connectionId)
        {
            try
            {
                var userKey = $"{UserConnectionsKeyPrefix}{userId}";

                // Use a Redis Set to store multiple connections per user
                await _redisDatabase.SetAddAsync(userKey, connectionId);

                // Set expiration to handle unexpected disconnections
                await _redisDatabase.KeyExpireAsync(userKey, TimeSpan.FromHours(24));

                // Also maintain a reverse mapping for quick lookup
                await _redisDatabase.StringSetAsync($"connection:{connectionId}", userId, TimeSpan.FromHours(24));

                // Update the main presence hash
                await _redisDatabase.HashSetAsync(PresenceHashKey, userId.ToString(), DateTime.UtcNow.Ticks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting user {UserId} with connection {ConnectionId}", userId, connectionId);
                throw;
            }
        }

        public async Task UserDisconnectedAsync(long userId, string connectionId)
        {
            try
            {
                var userKey = $"{UserConnectionsKeyPrefix}{userId}";

                // Remove the connection from the user's set
                await _redisDatabase.SetRemoveAsync(userKey, connectionId);

                // Remove reverse mapping
                await _redisDatabase.KeyDeleteAsync($"connection:{connectionId}");

                // Check if user has any remaining connections
                var remainingConnections = await _redisDatabase.SetLengthAsync(userKey);

                if (remainingConnections == 0)
                {
                    // No more connections, remove from presence hash
                    await _redisDatabase.HashDeleteAsync(PresenceHashKey, userId.ToString());
                    await _redisDatabase.KeyDeleteAsync(userKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting user {UserId} with connection {ConnectionId}", userId, connectionId);
                throw;
            }
        }

        public async Task<bool> IsOnlineAsync(long userId)
        {
            try
            {
                return await _redisDatabase.HashExistsAsync(PresenceHashKey, userId.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking online status for user {UserId}", userId);
                throw;
            }
        }

        public async Task<long[]> GetOnlineUserIdsAsync()
        {
            try
            {
                var allOnlineHashes = await _redisDatabase.HashKeysAsync(PresenceHashKey);

                return allOnlineHashes
                    .Select(hashValue => long.Parse(hashValue.ToString()))
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online user IDs");
                throw;
            }
        }

        // Additional method to get all connections for a user
        public async Task<string[]> GetUserConnectionsAsync(long userId)
        {
            try
            {
                var userKey = $"{UserConnectionsKeyPrefix}{userId}";
                var connections = await _redisDatabase.SetMembersAsync(userKey);
                return connections.Select(c => c.ToString()).ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connections for user {UserId}", userId);
                throw;
            }
        }
    }
}
