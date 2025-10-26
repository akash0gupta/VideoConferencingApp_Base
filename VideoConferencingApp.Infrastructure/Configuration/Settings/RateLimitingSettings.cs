using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.Configuration.Settings
{
    public class RateLimitingSettings:IConfig
    {
        public int MaxCallAttemptsPerMinute { get; set; } = 10;
        public int MaxConnectionAttemptsPerHour { get; set; } = 50;
        public int MaxIceCandidatesPerMinute { get; set; } = 100;
        public int BanDurationMinutes { get; set; } = 15;

        public string SectionName => "RateLimiting";
    }
}
