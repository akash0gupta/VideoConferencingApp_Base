using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Models
{
    public class CallOffer
    {
        public string FromUserId { get; set; } = string.Empty;
        public string FromUsername { get; set; } = string.Empty;
        public string SdpOffer { get; set; } = string.Empty;
        public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    }

}
