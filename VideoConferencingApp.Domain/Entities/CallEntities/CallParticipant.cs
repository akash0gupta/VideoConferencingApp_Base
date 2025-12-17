namespace VideoConferencingApp.Domain.Entities.CallEntities
{
    public class CallParticipant : BaseEntity
    {
        public string CallId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LeftAt { get; set; }
        public bool IsAudioEnabled { get; set; } = true;
        public bool IsVideoEnabled { get; set; } = true;
        public string? ConnectionId { get; set; }


    }
}