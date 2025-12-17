using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Domain.CacheKeys;
using VideoConferencingApp.Infrastructure.Configuration;
using VideoConferencingApp.Infrastructure.Configuration.Settings;

namespace VideoConferencingApp.Infrastructure.Caching
{
    public class InMemoryCacheManager : IStaticCacheManager
    {
        private readonly IMemoryCache _memory;
        private readonly CacheSettings _settings;

        private long _globalVersion = 0;
        private readonly ConcurrentDictionary<string, long> _prefixVersions = new(StringComparer.Ordinal);

        public InMemoryCacheManager(IMemoryCache memory, AppSettings appSettings)
        {
            _memory = memory;
            _settings = appSettings.Get<CacheSettings>();
        }

        public async Task<T> GetOrCreateAsync<T>(CacheKey key, Func<CancellationToken, Task<T>> acquire, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            var composedKey = ComposeKey(key);
            if (_memory.TryGetValue(composedKey, out T value))
                return value;

            var result = await acquire(cancellationToken).ConfigureAwait(false);

            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? GetDefaultTtl(),
                Size=1
            };

            _memory.Set(composedKey, result, options);
            return result;
        }

        public Task<T?> GetAsync<T>(CacheKey key, CancellationToken cancellationToken = default)
        {
            var composedKey = ComposeKey(key);
            if (_memory.TryGetValue(composedKey, out T value))
                return Task.FromResult<T?>(value);

            return Task.FromResult(default(T));
        }

        public Task SetAsync<T>(CacheKey key, T data, CancellationToken cancellationToken = default)
        {
            var composedKey = ComposeKey(key);
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = key.Expiry ?? GetDefaultTtl(),
                Size = 1
            };
            _memory.Set(composedKey, data, options);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(CacheKey key, CancellationToken cancellationToken = default)
        {
            var composedKey = ComposeKey(key);
            _memory.Remove(composedKey);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            _ = _prefixVersions.AddOrUpdate(prefix, 1, (_, v) => v + 1);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _globalVersion);
            // Clearing all items from IMemoryCache programmatically requires Compact.
            if (_memory is MemoryCache mc)
                mc.Compact(1.0);
            return Task.CompletedTask;
        }

        private string ComposeKey(CacheKey key)
        {
            var g = Interlocked.Read(ref _globalVersion);
            var prefixPart = string.Join("|", key.Prefixes
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(p => $"{p}:{(_prefixVersions.TryGetValue(p, out var v) ? v : 0)}"));

            return $"g:{g}|{prefixPart}|{key.Key}";
        }

        public TimeSpan GetDefaultTtl() => TimeSpan.FromSeconds(_settings.DefaultCacheTimeSeconds);
        public TimeSpan GetShortTtl() => TimeSpan.FromSeconds(_settings.ShortCacheTimeSeconds);
        public TimeSpan GetLongTtl() => TimeSpan.FromSeconds(_settings.LongCacheTimeSeconds);
    }
}