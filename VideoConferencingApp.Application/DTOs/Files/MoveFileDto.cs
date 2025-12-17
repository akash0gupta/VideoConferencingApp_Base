using System.ComponentModel.DataAnnotations;

namespace VideoConferencingApp.Application.DTOs.Files
{
    public class MoveFileDto
        {
            [Required]
            public long FileId { get; set; }

            public string TargetFolderId { get; set; }
        }
    
}
