using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Domain.Models
{
    public class ContactRelationship
    {
        public ContactStatus Status { get; set; }
        public bool IsSentByCurrentUser { get; set; }
    }
}
