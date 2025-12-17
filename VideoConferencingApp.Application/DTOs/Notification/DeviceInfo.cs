using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Notification
{
    public class DeviceInfo
    {
        public string DeviceToken { get; set; }  // FCM Token
        public DevicePlatform Platform { get; set; }
        public string DeviceId { get; set; }  // Unique device identifier
        public string DeviceName { get; set; }  // e.g., "iPhone 13 Pro"
        public string DeviceModel { get; set; }  // e.g., "SM-G991B"
        public string OsVersion { get; set; }  // e.g., "Android 13", "iOS 16.5"
        public string AppVersion { get; set; }  // e.g., "1.0.5"
    }

}
