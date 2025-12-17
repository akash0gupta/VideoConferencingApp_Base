using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.Configuration.Settings
{
    public class CommonConfig: IConfig
    {
        public  string SectionName => "CommonConfig";
        public string CorsDomains { get; set; } = string.Empty;
    }
}
