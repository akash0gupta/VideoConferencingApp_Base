using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Models
{
    public class CallAnswer
    {
        public string FromUserId { get; set; } = string.Empty;
        public string SdpAnswer { get; set; } = string.Empty;
        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    }
}
