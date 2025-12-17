using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Domain.CacheKeys;
using VideoConferencingApp.Infrastructure.Configuration;
using VideoConferencingApp.Infrastructure.Configuration.Settings;

namespace VideoConferencingApp.Infrastructure.Caching
{
    public class HybridCacheManager : IStaticCacheManager
    {
        private const string GlobalVersionKey = "cache:version:global";
        private static string PrefixVersionKey(string prefix) => $"cache:version:prefix:{prefix}";

        private readonly IMemoryCache _memory;
        private readonly IDistributedCache _distributed;
        private readonly CacheSettings _settings;

        public HybridCacheManager(IMemoryCache memory, IDistributedCache distributed, AppSettings appSettings)
        {
            _memory = memory;
            _distributed = distributed;
            _settings = appSettings.Get<CacheSettings>(); ;
        }

        public async Task<T> GetOrCreateAsync<T>(CacheKey key, Func<CancellationToken, Task<T>> acquire, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            var composedKey = await ComposeKeyAsync(key, cancellationToken).ConfigureAwait(false);

            if (_memory.TryGetValue(composedKey, out T cachedMem))
                return cachedMem;

            var bytes = await _distributed.GetAsync(composedKey, cancellationToken).ConfigureAwait(false);
            if (bytes is not null)
            {
                var distVal = SerializationHelper.FromBytes<T>(bytes);
                if (distVal is not null)
                {
                    _memory.Set(composedKey, distVal, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = ttl ?? GetDefaultTtl(),
                        Size = 1
                    });
                    return distVal;
                }
            }

            var data = await acquire(cancellationToken).ConfigureAwait(false);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? GetDefaultTtl()
            };
            await _distributed.SetAsync(composedKey, SerializationHelper.ToBytes(data), options, cancellationToken)
                              .ConfigureAwait(false);

            _memory.Set(composedKey, data, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? GetDefaultTtl(),
                Size=1
            });

            return data;
        }

        public async Task<T?> GetAsync<T>(CacheKey key, CancellationToken cancellationToken = default)
        {
            var composedKey = await ComposeKeyAsync(key, cancellationToken).ConfigureAwait(false);

            if (_memory.TryGetValue(composedKey, out T cached))
                return cached;

            var bytes = await _distributed.GetAsync(composedKey, cancellationToken).ConfigureAwait(false);
            var value = SerializationHelper.FromBytes<T>(bytes);
            if (value is not null)
            {
                _memory.Set(composedKey, value, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = GetDefaultTtl(),
                    Size = 1
                });
            }
            return value;
        }

        public async Task SetAsync<T>(CacheKey key, T data, CancellationToken cancellationToken = default)
        {
            var composedKey = await ComposeKeyAsync(key, cancellationToken).ConfigureAwait(false);

            var distOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = key.Expiry ?? GetDefaultTtl()
            };
            await _distributed.SetAsync(composedKey, SerializationHelper.ToBytes(data), distOptions, cancellationToken)
                              .ConfigureAwait(false);

            _memory.Set(composedKey, data, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = key.Expiry ?? GetDefaultTtl(),
                Size = 1
            });
        }

        public async Task RemoveAsync(CacheKey key, CancellationToken cancellationToken = default)
        {
            var composedKey = await ComposeKeyAsync(key, cancellationToken).ConfigureAwait(false);
            await _distributed.RemoveAsync(composedKey, cancellationToken).ConfigureAwait(false);
            _memory.Remove(composedKey);
        }

        public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            await IncrementVersionAsync(PrefixVersionKey(prefix), cancellationToken).ConfigureAwait(false);
            // Not clearing memory entries explicitly; versioning will prevent reuse of old entries.
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            await IncrementVersionAsync(GlobalVersionKey, cancellationToken).ConfigureAwait(false);
            if (_memory is MemoryCache mc)
                mc.Compact(1.0);
        }

        private async Task<string> ComposeKeyAsync(CacheKey key, CancellationToken ct)
        {
            var g = await GetVersionAsync(GlobalVersionKey, ct).ConfigureAwait(false);

            var prefixParts = key.Prefixes
                .OrderBy(p => p, StringComparer.Ordinal)
                .Select(async p => $"{p}:{await GetVersionAsync(PrefixVersionKey(p), ct).ConfigureAwait(false)}")
                .ToArray();

            var resolved = await Task.WhenAll(prefixParts).ConfigureAwait(false);
            var prefixPart = string.Join("|", resolved);

            return $"g:{g}|{prefixPart}|{key.Key}";
        }

        private async Task<long> GetVersionAsync(string versionKey, CancellationToken ct)
        {
            var bytes = await _distributed.GetAsync(versionKey, ct).ConfigureAwait(false);
            var str = SerializationHelper.FromBytesString(bytes);
            return long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }

        private async Task<long> IncrementVersionAsync(string versionKey, CancellationToken ct)
        {
            var current = await GetVersionAsync(versionKey, ct).ConfigureAwait(false);
            var next = current + 1;
            await _distributed.SetAsync(versionKey, SerializationHelper.ToBytesString(next.ToString(CultureInfo.InvariantCulture)),
                new DistributedCacheEntryOptions(), ct).ConfigureAwait(false);
            return next;
        }

        public TimeSpan GetDefaultTtl() => TimeSpan.FromSeconds(_settings.DefaultCacheTimeSeconds);
        public TimeSpan GetShortTtl() => TimeSpan.FromSeconds(_settings.ShortCacheTimeSeconds);
        public TimeSpan GetLongTtl() => TimeSpan.FromSeconds(_settings.LongCacheTimeSeconds);
    }
}