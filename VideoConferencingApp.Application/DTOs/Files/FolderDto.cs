using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Files
{
    public class FolderDto
        {
            public string FolderId { get; set; }
            public string FolderName { get; set; }
            public string ParentFolderId { get; set; }
            public string Path { get; set; }
            public FileVisibility Visibility { get; set; }
            public int FileCount { get; set; }
            public long TotalSize { get; set; }
            public string FormattedTotalSize { get; set; }
            public DateTime CreatedAt { get; set; }
            public List<FolderDto> SubFolders { get; set; }
        }
    
}
