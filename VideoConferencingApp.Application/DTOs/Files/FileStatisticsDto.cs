using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.DTOs.Files
{
    public class FileStatisticsDto
        {
            public int TotalFiles { get; set; }
            public long TotalSize { get; set; }
            public string FormattedTotalSize { get; set; }
            public long UsedStorage { get; set; }
            public string FormattedUsedStorage { get; set; }
            public long StorageLimit { get; set; }
            public string FormattedStorageLimit { get; set; }
            public double StorageUsagePercentage { get; set; }
            public Dictionary<string, int> FilesByType { get; set; }
            public Dictionary<FileVisibility, int> FilesByVisibility { get; set; }
            public int SharedFiles { get; set; }
            public int TotalDownloads { get; set; }
        }
    
}
