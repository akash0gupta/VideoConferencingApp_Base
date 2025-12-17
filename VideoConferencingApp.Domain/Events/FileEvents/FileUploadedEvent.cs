using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Events.FileEvents
{

    // Events for Event Publisher
    public class FileUploadedEvent : BaseEvent
    {
        public long FileId { get; set; }
        public long UserId { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
