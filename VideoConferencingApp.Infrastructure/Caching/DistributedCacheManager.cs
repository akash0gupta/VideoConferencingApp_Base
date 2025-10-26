using System.Globalization;
using Microsoft.Extensions.Caching.Distributed;
using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;
using VideoConferencingApp.Domain.CacheKeys;
using VideoConferencingApp.Infrastructure.Configuration;
using VideoConferencingApp.Infrastructure.Configuration.Settings;
using VideoConferencingApp.Infrastructure.Persistence;

namespace VideoConferencingApp.Infrastructure.Caching
{
    public class DistributedCacheManager : IStaticCacheManager
    {
        private const string GlobalVersionKey = "cache:version:global";
        private static string PrefixVersionKey(string prefix) => $"cache:version:prefix:{prefix}";

        private readonly IDistributedCache _distributed;
        private readonly CacheSettings _settings;

        public DistributedCacheManager(IDistributedCache distributed, AppSettings appSettings)
        {
            _distributed = distributed;
            _settings = appSettings.Get<CacheSettings>();
        }

        public async Task<T> GetOrCreateAsync<T>(CacheKey key, Func<CancellationToken, Task<T>> acquire, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            var composedKey = await ComposeKeyAsync(key, cancellationToken).ConfigureAwait(false);
            var bytes = await _distributed.GetAsync(composedKey, cancellationToken).ConfigureAwait(false);
            if (bytes is not null)
            {
                var fromCache = SerializationHelper.FromBytes<T>(bytes);
                if (fromCache is not null)
                    return fromCache;
            }

            var data = await acquire(cancellationToken).ConfigureAwait(false);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? GetDefaultTtl()
            };

            await _distributed.SetAsync(composedKey, SerializationHelper.ToBytes(data), options, cancellationToken)
                              .ConfigureAwait(false);
            return data;
        }

        public async Task<T?> GetAsync<T>(CacheKey key, CancellationToken cancellationToken = default)
        {
            var composedKey = await ComposeKeyAsync(key, cancellationToken).ConfigureAwait(false);
            var bytes = await _distributed.GetAsync(composedKey, cancellationToken).ConfigureAwait(false);
            return SerializationHelper.FromBytes<T>(bytes);
        }

        public async Task SetAsync<T>(CacheKey key, T data, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
        {
            var composedKey = await ComposeKeyAsync(key, cancellationToken).ConfigureAwait(false);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? GetDefaultTtl()
            };

            await _distributed.SetAsync(composedKey, SerializationHelper.ToBytes(data), options, cancellationToken)
                              .ConfigureAwait(false);
        }

        public async Task RemoveAsync(CacheKey key, CancellationToken cancellationToken = default)
        {
            var composedKey = await ComposeKeyAsync(key, cancellationToken).ConfigureAwait(false);
            await _distributed.RemoveAsync(composedKey, cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            // Bump the prefix "generation" to invalidate all keys carrying this prefix
            await IncrementVersionAsync(PrefixVersionKey(prefix), cancellationToken).ConfigureAwait(false);
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            await IncrementVersionAsync(GlobalVersionKey, cancellationToken).ConfigureAwait(false);
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
            // Note: not atomic across multiple instances, acceptable for most scenarios.
            var current = await GetVersionAsync(versionKey, ct).ConfigureAwait(false);
            var next = current + 1;
            await _distributed.SetAsync(versionKey,
                SerializationHelper.ToBytesString(next.ToString(CultureInfo.InvariantCulture)),
                new DistributedCacheEntryOptions(), ct).ConfigureAwait(false);
            return next;
        }

        public TimeSpan GetDefaultTtl() => TimeSpan.FromSeconds(_settings.DefaultCacheTimeSeconds);
        public TimeSpan GetShortTtl() => TimeSpan.FromSeconds(_settings.ShortCacheTimeSeconds);
        public TimeSpan GetLongTtl() => TimeSpan.FromSeconds(_settings.LongCacheTimeSeconds);
    }
}