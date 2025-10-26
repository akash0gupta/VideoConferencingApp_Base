using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using VideoConferencingApp.Infrastructure.Configuration.Settings;

namespace VideoConferencingApp.Infrastructure.Configuration.Redis
{

    public class RedisConnectionManager : IDisposable
    {
        private readonly ILogger<RedisConnectionManager> _logger;
        private readonly RedisSettings _settings;
        private readonly ConcurrentDictionary<string, ConnectionMultiplexer> _connections = new();

        public RedisConnectionManager(
            AppSettings appSettings,
            ILogger<RedisConnectionManager> logger)
        {
            _settings = appSettings.Get<RedisSettings>(); ;
            _logger = logger;

            // Eagerly connect to configured Redis instances
            foreach (var kvp in _settings.ConnectionStrings)
            {
                TryConnect(kvp.Key, kvp.Value);
            }
        }

        private void TryConnect(string name, string connectionString)
        {
            try
            {
                var connection = ConnectionMultiplexer.Connect(connectionString);
                _connections[name] = connection;
                _logger.LogInformation("Redis connection '{Name}' established", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis '{Name}'", name);
            }
        }

        public ConnectionMultiplexer? GetRedisConnection(string name)
        {
            if (_connections.TryGetValue(name, out var connection))
            {
                return connection;
            }

            // Fallback logic
            if (_settings.EnableRedisFallback && _settings.UseCacheRedisConnection && name == "presence")
            {
                _logger.LogWarning("Falling back to cache Redis connection for presence");
                return GetRedisConnection("cache");
            }

            return null;
        }

        public void Dispose()
        {
            foreach (var kvp in _connections)
            {
                try
                {
                    kvp.Value.Dispose();
                    _logger.LogInformation("Redis connection '{Name}' disposed", kvp.Key);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing Redis connection '{Name}'", kvp.Key);
                }
            }
        }
    }
}