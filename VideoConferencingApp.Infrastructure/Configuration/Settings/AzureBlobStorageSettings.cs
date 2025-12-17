namespace VideoConferencingApp.Infrastructure.Configuration.Settings
{
    public class AzureBlobStorageSettings
    {
        public required string ConnectionString { get; set; }
        public required string ContainerName { get; set; }
    }
}
