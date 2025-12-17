using System;
using System.Threading;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.CacheKeys;

namespace VideoConferencingApp.Application.Common.ICommonServices
{
    public interface IStaticCacheManager
    {
        Task<T> GetOrCreateAsync<T>(
            CacheKey key,
            Func<CancellationToken, Task<T>> acquire,
            TimeSpan? ttl = null,
            CancellationToken cancellationToken = default);

        Task<T?> GetAsync<T>(CacheKey key, CancellationToken cancellationToken = default);

        Task SetAsync<T>(CacheKey key, T data,CancellationToken cancellationToken = default);

        Task RemoveAsync(CacheKey key, CancellationToken cancellationToken = default);

        Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

        Task ClearAsync(CancellationToken cancellationToken = default);

        TimeSpan GetDefaultTtl();
        TimeSpan GetShortTtl();
        TimeSpan GetLongTtl();
    }
}