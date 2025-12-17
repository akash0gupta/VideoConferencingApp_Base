using System.ComponentModel.DataAnnotations;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Files
{
    public class CreateFolderDto
        {
            [Required]
            [StringLength(255)]
            public string FolderName { get; set; }

            public string ParentFolderId { get; set; }

            public FileVisibility Visibility { get; set; } = FileVisibility.Private;
        }
    
}
