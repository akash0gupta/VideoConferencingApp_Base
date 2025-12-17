using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Entities.FileEntities
{
    public class TFileAccess : BaseEntity
    {
        public long FileId { get; set; }
        public long? UserId { get; set; }
        public string AccessToken { get; set; }
        public FileAccessType AccessType { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime AccessedAt { get; set; }
    }

}
