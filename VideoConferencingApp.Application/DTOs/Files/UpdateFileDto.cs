using System.ComponentModel.DataAnnotations;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Files
{
    public class UpdateFileDto
        {
            [StringLength(255)]
            public string FileName { get; set; }

            [StringLength(500)]
            public string Description { get; set; }

            public FileVisibility? Visibility { get; set; }

            [StringLength(500)]
            public string Tags { get; set; }

            public string FolderId { get; set; }
        }
    
}
