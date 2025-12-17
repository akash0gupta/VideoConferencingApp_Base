using System;

namespace VideoConferencingApp.Domain.CacheKeys
{
    public static class AppCacheKey
    {
        public const string GlobalPrefix = "app";

        public static CacheKey Build(params string[] parts)
            => new CacheKey(string.Join(":", parts),null,GlobalPrefix);
    }
}