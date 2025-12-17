using System.ComponentModel.DataAnnotations;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Files
{
    public class CreateFileShareDto
        {
            [Required]
            public long FileId { get; set; }

            public long? SharedWithUserId { get; set; }

            public FilePermission Permission { get; set; } = FilePermission.View;

            public DateTime? ExpiresAt { get; set; }

            public string Password { get; set; }

            public int? MaxAccessCount { get; set; }
        }
    
}
