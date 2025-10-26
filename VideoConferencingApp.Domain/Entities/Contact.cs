using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Entities
{
    public class Contact : BaseEntity
    {
        public long RequesterId { get; set; }
        public string RequesterUserName { get; set; }
        public long AddresseeId { get; set; }
        public string AddresseeUserName { get; set; }
        public ContactStatus Status { get; set; }
        public string Message { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string RejectionReason { get; set; }
        public DateTime? BlockedAt { get; set; }
        public string BlockReason { get; set; }
        public bool IsFavorite { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public long? DeletedBy { get; set; }
    }
}
