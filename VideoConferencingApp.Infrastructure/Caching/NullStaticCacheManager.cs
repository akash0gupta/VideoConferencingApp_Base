using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Domain.CacheKeys;
using VideoConferencingApp.Infrastructure.Configuration;
using VideoConferencingApp.Infrastructure.Configuration.Settings;

namespace VideoConferencingApp.Infrastructure.Caching
{
    public class NullStaticCacheManager : IStaticCacheManager
    {
        private readonly CacheSettings _settings;

        public NullStaticCacheManager(AppSettings appSettings) => _settings = appSettings.Get<CacheSettings>();

        public Task ClearAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T?> GetAsync<T>(CacheKey key, CancellationToken cancellationToken = default)
            => Task.FromResult(default(T));

        public Task<T> GetOrCreateAsync<T>(CacheKey key, Func<CancellationToken, Task<T>> acquire, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
            => acquire(cancellationToken);

        public Task RemoveAsync(CacheKey key, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SetAsync<T>(CacheKey key, T data,CancellationToken cancellationToken = default) => Task.CompletedTask;

        public TimeSpan GetDefaultTtl() => TimeSpan.FromSeconds(_settings.DefaultCacheTimeSeconds);
        public TimeSpan GetShortTtl() => TimeSpan.FromSeconds(_settings.ShortCacheTimeSeconds);
        public TimeSpan GetLongTtl() => TimeSpan.FromSeconds(_settings.LongCacheTimeSeconds);
    }
}