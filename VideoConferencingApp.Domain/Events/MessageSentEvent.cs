using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Events;

namespace VideoConferencingApp.Application.Events
{
    public class MessageSentEvent : BaseEvent
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public string RoomId { get; set; }
        public string Content { get; set; }
        public Guid EventId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
