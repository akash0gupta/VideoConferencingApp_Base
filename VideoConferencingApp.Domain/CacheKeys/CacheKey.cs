using System;
using System.Collections.Generic;
using System.Linq;

namespace VideoConferencingApp.Domain.CacheKeys
{
    public sealed class CacheKey
    {
        public CacheKey(string key, TimeSpan? expiry = null, params string[] prefixes)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null or whitespace.", nameof(key));

            Key = key;
            Prefixes = prefixes?
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.Ordinal)
                .ToArray() ?? Array.Empty<string>();
        }



        public string Key { get; }

        /// <summary>
        /// Gets the expiration time for the cache entry
        /// </summary>
        public TimeSpan? Expiry { get; }

        public IReadOnlyList<string> Prefixes { get; }

        public CacheKey WithPrefix(params string[] prefixes)
        {
            var merged = Prefixes.Concat(prefixes ?? Array.Empty<string>())
                                 .Where(p => !string.IsNullOrWhiteSpace(p))
                                 .Distinct(StringComparer.Ordinal)
                                 .ToArray();

            return new CacheKey(Key,null, merged);
        }

        public static CacheKey With(string key, params string[] prefixes) => new CacheKey(key,null, prefixes);

        public override string ToString() => Key;
    }
}