using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.CacheKeys
{
    /// <summary>
    /// Centralized cache key management for contact-related operations
    /// </summary>
    public class ContactCacheKey
    {
        #region Constants
        private const string PrefixRaw = "contacts";
        #endregion

        public static string Prefix => PrefixRaw;

        #region Contact Keys

        /// <summary>
        /// Cache key for user's contacts list
        /// </summary>
        public static CacheKey UserContacts(long userId) =>
            new($"{Prefix}.user.{userId}");

        /// <summary>
        /// Cache key for a specific contact
        /// </summary>
        public static CacheKey Contact(long userId, long contactId) =>
            new($"{Prefix}.detail.{userId}.{contactId}");

        /// <summary>
        /// Cache key for pending contact requests
        /// </summary>
        public static CacheKey PendingRequests(long userId) =>
            new($"{Prefix}.pending.{userId}");

        /// <summary>
        /// Cache key for sent contact requests
        /// </summary>
        public static CacheKey SentRequests(long userId) =>
            new($"{Prefix}.sent.{userId}");

        /// <summary>
        /// Cache key for received contact requests
        /// </summary>
        public static CacheKey ReceivedRequests(long userId) =>
            new($"{Prefix}.received.{userId}", TimeSpan.FromMinutes(5));

        /// <summary>
        /// Cache key for blocked users
        /// </summary>
        public static CacheKey BlockedUsers(long userId) =>
            new($"{Prefix}.blocked.{userId}", TimeSpan.FromMinutes(15));

        /// <summary>
        /// Cache key for contact search results
        /// </summary>
        public static CacheKey SearchResults(long userId, string query) =>
            new($"{Prefix}.search.{userId}.{query.GetHashCode()}", TimeSpan.FromMinutes(3));

        /// <summary>
        /// Cache key for quick search results
        /// </summary>
        public static CacheKey QuickSearch(long userId, string query) =>
            new($"{Prefix}.quick.search.{userId}.{query.GetHashCode()}", TimeSpan.FromMinutes(2));

        /// <summary>
        /// Cache key for contact relationship status between two users
        /// </summary>
        public static CacheKey RelationshipStatus(long userId1, long userId2) =>
            new($"{Prefix}relationship.{Math.Min(userId1, userId2)}.{Math.Max(userId1, userId2)}", TimeSpan.FromMinutes(10));

        /// <summary>
        /// Cache key for mutual contacts between two users
        /// </summary>
        public static CacheKey MutualContacts(long userId1, long userId2) =>
            new($"{Prefix}mutual.{Math.Min(userId1, userId2)}.{Math.Max(userId1, userId2)}", TimeSpan.FromMinutes(15));

        /// <summary>
        /// Cache key for contact suggestions
        /// </summary>
        public static CacheKey Suggestions(long userId) =>
            new($"{Prefix}suggestions.{userId}", TimeSpan.FromHours(1));

        /// <summary>
        /// Cache key for contact favorites
        /// </summary>
        public static CacheKey Favorites(long userId) =>
            new($"{Prefix}favorites.{userId}", TimeSpan.FromMinutes(15));

        #endregion

      
    }
}
