using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.DTOs.Files;
using VideoConferencingApp.Domain.Interfaces;

namespace VideoConferencingApp.Application.Services.FileServices
{
    public interface IUserFileManagerService
    {
        // File operations
        Task<FileDto> UploadFileAsync(long userId, FileUploadDto dto);
        Task<Stream> DownloadFileAsync(long fileId, long userId);
        Task<FileDto> GetFileAsync(long fileId, long userId);
        Task<IPagedList<FileDto>> GetUserFilesAsync(long userId, FileSearchDto searchDto);
        Task<FileDto> UpdateFileAsync(long fileId, long userId, UpdateFileDto dto);
        Task<bool> DeleteFileAsync(long fileId, long userId);
        Task<bool> DeleteFilesAsync(List<long> fileIds, long userId);

        // Folder operations
        Task<FolderDto> CreateFolderAsync(long userId, CreateFolderDto dto);
        Task<List<FolderDto>> GetUserFoldersAsync(long userId);
        Task<bool> DeleteFolderAsync(string folderId, long userId);
        Task<FileDto> MoveFileAsync(long userId, MoveFileDto dto);

        // Sharing operations
        Task<FileShareDto> ShareFileAsync(long userId, CreateFileShareDto dto);
        Task<List<FileShareDto>> GetFileSharesAsync(long fileId, long userId);
        Task<bool> RevokeShareAsync(long shareId, long userId);
        Task<Stream> DownloadSharedFileAsync(string shareToken, string password = null);
        Task<FileDto> GetSharedFileInfoAsync(string shareToken, string password = null);

        // Access and statistics
        Task<List<FileAccessDto>> GetFileAccessLogsAsync(long fileId, long userId);
        Task<FileStatisticsDto> GetUserStatisticsAsync(long userId);
        Task<bool> ValidateUserStorageAsync(long userId, long fileSize);

        // Bulk operations
        Task<bool> BulkOperationAsync(long userId, BulkFileOperationDto dto);
    }
}
