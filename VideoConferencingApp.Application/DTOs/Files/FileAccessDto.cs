using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Files
{
    public class FileAccessDto
        {
            public long Id { get; set; }
            public long FileId { get; set; }
            public string FileName { get; set; }
            public long? UserId { get; set; }
            public string UserName { get; set; }
            public FileAccessType AccessType { get; set; }
            public string IpAddress { get; set; }
            public DateTime AccessedAt { get; set; }
        }
    
}
