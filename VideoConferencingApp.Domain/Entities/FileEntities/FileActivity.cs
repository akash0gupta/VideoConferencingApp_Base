using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Entities.UserEntities;

namespace VideoConferencingApp.Domain.Entities.FileEntities
{
    public class FileActivity : BaseEntity
    {
        public long FileId { get; set; }
        public long? UserId { get; set; }
        public string ActivityType { get; set; }
        public string Details { get; set; }
        public string IpAddress { get; set; }
        public DateTime OccurredAt { get; set; }
    }
}
