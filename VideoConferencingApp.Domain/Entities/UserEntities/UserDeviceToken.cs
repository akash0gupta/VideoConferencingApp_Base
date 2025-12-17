using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Entities.UserEntities
{
    public class UserDeviceToken:BaseEntity,ISoftDeletedEntity
    {
        public long UserId { get; set; }
        public string DeviceToken { get; set; }
        public DevicePlatform Platform { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string DeviceModel { get; set; }
        public string OsVersion { get; set; }
        public string AppVersion { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public bool IsDeleted { get; set;}
    }
}
