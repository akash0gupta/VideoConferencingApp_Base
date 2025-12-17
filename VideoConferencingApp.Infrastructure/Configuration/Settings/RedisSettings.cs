using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.Configuration.Settings
{
    public class RedisSettings: IConfig
    {
        /// <summary>
        /// A key for the configuration section.
        /// </summary>
        public  string SectionName => "RedisSettings";
        public Dictionary<string, string> ConnectionStrings { get; set; } = new();
        public bool EnableRedisFallback { get; set; }
        public bool UseCacheRedisConnection { get; set; }
        public int PresenceExpirationHours { get; set; } = 24;
        public string RedisInstanceName { get; set; } = "vcapp";
    }
}
