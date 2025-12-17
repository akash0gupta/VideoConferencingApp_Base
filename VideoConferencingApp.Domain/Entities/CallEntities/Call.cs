using Microsoft.VisualBasic;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Entities.CallEntities
{
    public class Call : BaseEntity,ISoftDeletedEntity
    {
        public string CallerId { get; set; } = string.Empty;
        public string? ReceiverId { get; set; }
        public string? GroupId { get; set; }
        public CallsType Type { get; set; }
        public CallStatus Status { get; set; } = CallStatus.Initiating;
        public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ConnectedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int DurationSeconds { get; set; }
        public string? EndReason { get; set; }
        public string? Metadata { get; set; } // JSON for quality metrics, etc.
       public  bool IsDeleted { get; set; }
       public  long DeletedBy { get; set; }
    }
}