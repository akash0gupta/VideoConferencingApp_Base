namespace VideoConferencingApp.Domain.Events.FileEvents
{
    public class FileSharedEvent : BaseEvent
    {
        public long FileId { get; set; }
        public long ShareId { get; set; }
        public long SharedById { get; set; }
        public long? SharedWithUserId { get; set; }
        public string ShareToken { get; set; }
        public DateTime SharedAt { get; set; }
    }
}
