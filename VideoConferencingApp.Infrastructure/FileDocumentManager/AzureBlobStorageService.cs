using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.FileDocumentManager
{
    public class AzureBlobStorageService : IFileStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobStorageService(string connectionString, string containerName)
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            _containerClient.CreateIfNotExists();
        }

        public async Task<Stream> GetFileAsync(string filePath)
        {
            var blobClient = _containerClient.GetBlobClient(filePath);
            if (!await blobClient.ExistsAsync())
            {
                throw new FileNotFoundException("Blob not found.", filePath);
            }
            var response = await blobClient.DownloadStreamingAsync();
            return response.Value.Content;
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            var blobClient = _containerClient.GetBlobClient(uniqueFileName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            await blobClient.UploadAsync(fileStream, blobHttpHeaders);
            return blobClient.Uri.ToString(); // Return the full URI
        }

        public async Task DeleteFileAsync(string filePath)
        {
            var blobClient = _containerClient.GetBlobClient(filePath);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}
