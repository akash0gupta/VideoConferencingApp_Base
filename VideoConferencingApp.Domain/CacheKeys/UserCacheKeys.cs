using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.CacheKeys
{
    /// <summary>
    /// Cache keys for User entity
    /// </summary>
    public static class UserCacheKeys
    {
        /// <summary>
        /// Cache key prefix for all user-related cache entries
        /// </summary>
        public static string PrefixRaw => "user";

        /// <summary>
        /// Cache key for user by ID
        /// </summary>
        public static CacheKey ById(long userId) => new($"{PrefixRaw}_id_{userId}");

        /// <summary>
        /// Cache key for user by email
        /// </summary>
        public static CacheKey ByEmail(string email) => new($"{PrefixRaw}_email_{email.ToLower()}");

        /// <summary>
        /// Cache key for user by username
        /// </summary>
        public static CacheKey ByUsername(string username) => new($"{PrefixRaw}_username_{username.ToLower()}");

        /// <summary>
        /// Cache key for all users list
        /// </summary>
        public static CacheKey All => new($"{PrefixRaw}_all");

        /// <summary>
        /// Cache key for active users
        /// </summary>
        public static CacheKey AllActive => new($"{PrefixRaw}_all_active");

        /// <summary>
        /// Cache key for online users
        /// </summary>
        public static CacheKey OnlineUsers => new($"{PrefixRaw}_online");

        /// <summary>
        /// Cache key for user count
        /// </summary>
        public static CacheKey Count => new($"{PrefixRaw}_count");

        /// <summary>
        /// Cache key for users by role
        /// </summary>
        public static CacheKey ByRole(UserRole role) => new($"{PrefixRaw}_role_{role}");
    }
}