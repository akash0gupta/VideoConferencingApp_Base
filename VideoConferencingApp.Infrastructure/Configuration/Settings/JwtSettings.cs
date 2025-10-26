using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.Configuration.Settings
{
    public class JwtSettings: IConfig
    {
        public  string SectionName => "JwtSettings";
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpiryInMinutes { get; set; }
        public double RefreshTokenExpiryInDays { get; set; }
    }
}