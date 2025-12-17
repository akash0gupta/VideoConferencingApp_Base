using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Entities.FileEntities
{
    public class UserFolder : BaseEntity, ISoftDeletedEntity
    {
        public long UserId { get; set; }
        public string FolderId { get; set; }
        public string FolderName { get; set; }
        public string ParentFolderId { get; set; }
        public string Path { get; set; }
        public FileVisibility Visibility { get; set; }
        public bool IsDeleted { get; set; }
    }
}
