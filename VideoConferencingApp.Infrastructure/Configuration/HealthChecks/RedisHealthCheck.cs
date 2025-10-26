using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VideoConferencingApp.Infrastructure.Configuration.Redis;
using VideoConferencingApp.Infrastructure.Configuration.Settings;

namespace VideoConferencingApp.Infrastructure.Configuration.HealthChecks
{
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly RedisConnectionManager _connectionManager;
        private readonly ILogger<RedisHealthCheck> _logger;

        public RedisHealthCheck(RedisConnectionManager connectionManager,ILogger<RedisHealthCheck> logger)
        {
            _connectionManager = connectionManager;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var data = new Dictionary<string, object>();
                var unhealthyConnections = new List<string>();

                // Check cache connection
                var cacheConnection = _connectionManager.GetRedisConnection(CacheSettings.CacheConnectionKey);
                if (cacheConnection != null)
                {
                    if (cacheConnection.IsConnected)
                    {
                        var db = cacheConnection.GetDatabase();
                        await db.PingAsync();
                        data["cache"] = "connected";
                    }
                    else
                    {
                        unhealthyConnections.Add("cache");
                    }
                }

                // Check presence connection
                var presenceConnection = _connectionManager.GetRedisConnection(CacheSettings.PresenceConnectionKey);
                if (presenceConnection != null && presenceConnection != cacheConnection)
                {
                    if (presenceConnection.IsConnected)
                    {
                        var db = presenceConnection.GetDatabase();
                        await db.PingAsync();
                        data["presence"] = "connected";
                    }
                    else
                    {
                        unhealthyConnections.Add("presence");
                    }
                }

                if (unhealthyConnections.Any())
                {
                    return HealthCheckResult.Degraded(
                        $"Redis connections unhealthy: {string.Join(", ", unhealthyConnections)}",
                        data: data);
                }

                return HealthCheckResult.Healthy("All Redis connections are healthy", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                return HealthCheckResult.Unhealthy("Redis health check failed", ex);
            }
        }
    }
}