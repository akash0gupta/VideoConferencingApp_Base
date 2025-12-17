using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.FileDocumentManager
{
    public class LocalStorageService : IFileStorageService
    {
        private readonly string _basePath;

        public LocalStorageService(string basePath)
        {
            _basePath = basePath;
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public Task<Stream> GetFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_basePath, filePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("File not found.", fullPath);
            }
            return Task.FromResult<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
        }

        public Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var fullPath = Path.Combine(_basePath, uniqueFileName);

            using (var fileStreamOutput = new FileStream(fullPath, FileMode.Create))
            {
                fileStream.CopyTo(fileStreamOutput);
            }

            return Task.FromResult(uniqueFileName); // Return the relative path
        }

        public Task DeleteFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_basePath, filePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
            return Task.CompletedTask;
        }
    }
}
