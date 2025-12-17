using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.Configuration.Settings
{
    public enum StorageProvider { azureblob, local }
    public class FileDocumentSettings : IConfig
    {
        public string SectionName => "FileStorage";
        public string PathFolder { get; set; } = string.Empty;
        public int MaxFileSizeInMB { get; set; }
        public StorageProvider StorageProvider { get; set; }
        public AzureBlobStorageSettings? azureBlobStorageSettings { get; set; }
    }
   
}
