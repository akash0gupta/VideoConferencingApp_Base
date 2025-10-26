using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Entities;

namespace VideoConferencingApp.Domain.Events.ContactEvents
{
    public class ContactRequestAcceptedEvent : BaseEvent
    {
        public Contact Contact { get; set; }
        public string RequesterName { get; set; }
        public string AddresseeName { get; set; }
        public string RequesterEmail { get; set; }
        public string AddresseeEmail { get; set; }
        public string RequesterPhoneNumber { get; set; }
    }
}
