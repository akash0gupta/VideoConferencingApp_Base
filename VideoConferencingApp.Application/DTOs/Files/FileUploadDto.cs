using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Files
{
        public class FileUploadDto
        {
            [Required]
            public IFormFile File { get; set; }

            [StringLength(500)]
            public string ? Description { get; set; }

            public FileVisibility Visibility { get; set; } = FileVisibility.Private;

            public string ? FolderId { get; set; }

            
            public string ? Tags { get; set; }

            public DateTime? ExpiresAt { get; set; }

            public bool Encrypt { get; set; }
        }
    
}
