using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Infrastructure.Configuration.Settings
{
    public class NotificationSettings
    {
        public const string SectionName = "NotificationSettings";

        public bool EnableEmailNotifications { get; set; }
        public bool EnableSmsNotifications { get; set; }
        public bool EnablePushNotifications { get; set; }
        public bool EnableInAppNotifications { get; set; }
        public int NotificationRetentionDays { get; set; }
        public int BatchSize { get; set; }
        public int MaxRetries { get; set; }
        public int RetryDelaySeconds { get; set; } = 30;
        public bool EnableBatching { get; set; } = true;
        public bool EnablePriority { get; set; } = true;
        public int HighPriorityThreshold { get; set; } = 100;
        public Dictionary<string, NotificationChannelSettings> Channels { get; set; }
        public Dictionary<string, NotificationTemplate> Templates { get; set; }
    }
    public class NotificationChannelSettings
    {
        public bool Enabled { get; set; }
        public int Priority { get; set; }
        public int RateLimit { get; set; }
        public string Provider { get; set; }
        public Dictionary<string, string> ProviderSettings { get; set; }
    }

    public class NotificationTemplate
    {
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string HtmlBody { get; set; }
        public NotificationType Type { get; set; }
        public List<string> DefaultChannels { get; set; }
        public Dictionary<string, string> Variables { get; set; }
    }
}
