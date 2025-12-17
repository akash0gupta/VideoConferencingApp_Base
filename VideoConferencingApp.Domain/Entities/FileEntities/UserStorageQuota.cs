namespace VideoConferencingApp.Domain.Entities.FileEntities
{
    public class UserStorageQuota : BaseEntity
    {
        public long UserId { get; set; }
        public long StorageLimit { get; set; } // in bytes
        public long UsedStorage { get; set; } // in bytes
        public DateTime? LastCalculatedAt { get; set; }
    }
}
