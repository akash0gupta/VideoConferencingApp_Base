namespace VideoConferencingApp.Domain.Events.FileEvents
{
    public class FileDeletedEvent : BaseEvent
    {
        public long FileId { get; set; }
        public long UserId { get; set; }
        public string FileName { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
