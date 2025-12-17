using System.ComponentModel.DataAnnotations;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Files
{
    public class BulkFileOperationDto
        {
            [Required]
            public List<long> FileIds { get; set; }

            [Required]
            public BulkOperation Operation { get; set; }

            public FileVisibility? Visibility { get; set; }
            public string TargetFolderId { get; set; }
        }
    
}
