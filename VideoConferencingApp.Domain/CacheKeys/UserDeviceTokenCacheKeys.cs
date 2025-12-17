using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.CacheKeys
{
    public static class UserDeviceTokenCacheKeys
    {
        public const string PrefixRaw = "userdevicetoken.";
        public static string Prefix => PrefixRaw;

        public static CacheKey ById(long id) => new ($"{Prefix}id-{id}");
        public static CacheKey ByUserId(long userId) => new($"{Prefix}userid-{userId}");
        public static CacheKey ByDeviceToken(string deviceToken) =>  new($"{Prefix}token-{deviceToken}");
        public static CacheKey ByUserIdAndPlatform(long userId, DevicePlatform platform) =>
            new($"{Prefix}userid-{userId}-platform-{platform}");
        public static CacheKey ActiveByUserId(long userId) => new($"{Prefix}active-userid-{userId}");
    }
}
