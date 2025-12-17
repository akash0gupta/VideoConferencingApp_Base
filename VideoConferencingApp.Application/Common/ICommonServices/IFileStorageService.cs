using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.Common.ICommonServices
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves a file stream to the configured storage medium.
        /// </summary>
        /// <param name="fileStream">The stream of the file to save.</param>
        /// <param name="fileName">The original file name.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <returns>A unique path or URI to the saved file.</returns>
        Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType);

        /// <summary>
        /// Retrieves a file stream from the storage medium.
        /// </summary>
        /// <param name="filePath">The unique path or URI of the file.</param>
        /// <returns>A stream containing the file data.</returns>
        Task<Stream> GetFileAsync(string filePath);

        /// <summary>
        /// Deletes a file from the storage medium.
        /// </summary>
        /// <param name="filePath">The unique path or URI of the file.</param>
        Task DeleteFileAsync(string filePath);
    }
}
