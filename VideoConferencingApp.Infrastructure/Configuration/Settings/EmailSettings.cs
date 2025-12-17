using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.Configuration.Settings
{
    public class EmailSettings:IConfig
    {
        public string FromAddress { get; set; }
        public string FromName { get; set; }
        public SmtpSettings Smtp { get; set; }
        public EmailTemplates Templates { get; set; }

        public string SectionName => "EmailSettings";
    }
    public class EmailTemplates
    {
        public string EmailVerification { get; set; }
        public string PasswordReset { get; set; }
        public string Welcome { get; set; }
        public string TwoFactorCode { get; set; }
        public string AccountLocked { get; set; }
        public string PasswordChanged { get; set; }
    }

    public class SmtpSettings
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool UseSsl { get; set; }
        public bool UseStartTls { get; set; }
        public bool AllowInvalidCertificate { get; set; }
    }
}
