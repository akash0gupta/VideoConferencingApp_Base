using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Files
{
    public class FileSearchDto
        {
            public string ?Query { get; set; }
            public FileVisibility? Visibility { get; set; }
            public string ?FileExtension { get; set; }
            public string ?ContentType { get; set; }
            public string ?Tags { get; set; }
            public string ?FolderId { get; set; }
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public long? MinSize { get; set; }
            public long? MaxSize { get; set; }
            public int PageNumber { get; set; } = 1;
            public int PageSize { get; set; } = 20;
            public string OrderBy { get; set; } = "CreatedAt";
            public bool OrderDescending { get; set; } = true;
        }
    
}
