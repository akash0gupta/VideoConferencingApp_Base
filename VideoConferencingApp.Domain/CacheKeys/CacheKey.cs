using System;
using System.Collections.Generic;
using System.Linq;

namespace VideoConferencingApp.Domain.CacheKeys
{
    public sealed class CacheKey
    {
        public CacheKey(string key, params string[] prefixes)
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
        public IReadOnlyList<string> Prefixes { get; }

        public CacheKey WithPrefix(params string[] prefixes)
        {
            var merged = Prefixes.Concat(prefixes ?? Array.Empty<string>())
                                 .Where(p => !string.IsNullOrWhiteSpace(p))
                                 .Distinct(StringComparer.Ordinal)
                                 .ToArray();

            return new CacheKey(Key, merged);
        }

        public static CacheKey With(string key, params string[] prefixes) => new CacheKey(key, prefixes);

        public override string ToString() => Key;
    }
}