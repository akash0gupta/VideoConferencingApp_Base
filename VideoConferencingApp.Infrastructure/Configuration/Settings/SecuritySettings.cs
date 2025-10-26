using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.Configuration.Settings
{
    public class SecuritySettings:IConfig
    {
        public string SectionName => "SecuritySettings";
        public bool RequireEmailVerification { get; set; }
        public int MinPasswordLength { get; set; }
        public bool RequireUppercase { get; set; }
        public bool RequireLowercase { get; set; }
        public bool RequireDigit { get; set; }
        public bool RequireSpecialCharacter { get; set; }
        public int PasswordExpiryDays { get; set; }
        public bool PreventPasswordReuse { get; set; }
        public bool RotateRefreshTokens { get; set; }
        public string BaseUrl { get; set; }
        public string EncryptionKey { get; set; }
        public string AppName { get; set; }
    }

}
