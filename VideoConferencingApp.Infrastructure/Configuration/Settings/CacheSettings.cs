using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.Configuration.Settings
{
    public enum CacheProviderType { None, InMemory, Distributed, Hybrid }
    /// <summary>
    /// Represents the cache settings that are bound from application configuration.
    /// </summary>
    public class CacheSettings:IConfig
    {
        public  string SectionName => "CacheSettings";

        public const string CacheConnectionKey = "cache";
        public const string PresenceConnectionKey = "presence";

        /// <summary>
        /// The type of cache provider to use (InMemory, Distributed, Hybrid, or None).
        /// </summary>
        public CacheProviderType CacheProvider { get; set; } = CacheProviderType.None;
        public CacheProviderType PresenceProviderType { get; set; } = CacheProviderType.None;

        /// <summary>
        /// The default cache expiration in seconds for standard items.
        /// </summary>
        public int DefaultCacheTimeSeconds { get; set; } = 300; // 5 minutes

        /// <summary>
        /// A shorter cache expiration in seconds for frequently changing items.
        /// </summary>
        public int ShortCacheTimeSeconds { get; set; } = 60; // 1 minute

        /// <summary>
        /// A longer cache expiration in seconds for rarely changing items.
        /// </summary>
        public int LongCacheTimeSeconds { get; set; } = 3600; // 1 hour

        /// <summary>
        /// The maximum size of the in-memory (L1) cache in megabytes. If 0, there is no limit.
        /// </summary>
        public int MemorySizeLimitMB { get; set; } = 0; // Default to no limit

        // --- ICacheSettings Implementation ---
        // We'll have this return the default time in minutes, as per our previous interface.
        public int DefaultCacheTime => DefaultCacheTimeSeconds / 60;


    }
}