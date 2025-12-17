using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.CacheKeys
{
    public class AuthCacheKey
    {
        private const string PrefixRaw = "auth";
        private const string PrefixRawRate = "RateLimit";

        private const int MAX_LOGIN_ATTEMPTSRow = 5;
        private const int LOCKOUT_DURATION_MINUTESRow = 30;
        private const int PASSWORD_RESET_TOKEN_EXPIRY_HOURSRow = 24;
        private const int EMAIL_VERIFICATION_TOKEN_EXPIRY_DAYSRow = 7;

        public static int MAX_LOGIN_ATTEMPTS => MAX_LOGIN_ATTEMPTSRow;
        public static int LOCKOUT_DURATION_MINUTES=>LOCKOUT_DURATION_MINUTESRow;
        public static int PASSWORD_RESET_TOKEN_EXPIRY_HOURS => PASSWORD_RESET_TOKEN_EXPIRY_HOURSRow;
        public static int EMAIL_VERIFICATION_TOKEN_EXPIRY_DAYS => EMAIL_VERIFICATION_TOKEN_EXPIRY_DAYSRow;

        public static string Prefix => PrefixRaw;
        public static string PrefixRate => PrefixRaw;

        public static CacheKey RateLimit(string key, TimeSpan period) =>
            new($"{PrefixRate}.{key}", period);

        public static CacheKey TwoFactorCode(long userId) =>
            new($"{Prefix}.2fa_code.{userId}");

        public static CacheKey UserPermissions(long userId) =>
            new($"{Prefix}.user_permissions.{userId}");

        public static CacheKey UserSession(string sessionId) =>
            new($"{Prefix}.session.{sessionId}");

        public static CacheKey UserSessions(long userId) =>
            new($"{Prefix}.user_sessions.{userId}");

        public static CacheKey RefreshToken(string token) =>
            new($"{Prefix}.refresh_token.{token}");

        public static CacheKey UserRefreshTokens(long userId) =>
            new($"{Prefix}.user_refresh_tokens.{userId}");
    }
}
