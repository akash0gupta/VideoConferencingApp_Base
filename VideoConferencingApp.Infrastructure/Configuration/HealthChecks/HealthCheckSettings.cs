using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Infrastructure.Configuration.HealthChecks
{
    public class HealthCheckSettings
    {
        public const string SectionName = "HealthChecks";
        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }

}
