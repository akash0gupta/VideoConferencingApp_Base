using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.Services.FileServices
{
    public interface IFileManagerService
    {
        // Core file operations
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, string folderPath = null);
        Task<Stream> GetFileAsync(string filePath);
        Task<bool> DeleteFileAsync(string filePath);
        Task<bool> FileExistsAsync(string filePath);
        Task<FileInfo> GetFileInfoAsync(string filePath);
        Task<string> CopyFileAsync(string sourcePath, string destinationPath);
        Task<string> MoveFileAsync(string sourcePath, string destinationPath);

        // Batch operations
        Task<List<string>> SaveMultipleFilesAsync(Dictionary<Stream, string> files, string folderPath = null);
        Task<bool> DeleteMultipleFilesAsync(List<string> filePaths);

        // Storage info
        Task<long> GetFileSizeAsync(string filePath);
        Task<string> GenerateThumbnailAsync(string filePath, int width, int height);
        Task<string> GetFileUrlAsync(string filePath);
    }
}
