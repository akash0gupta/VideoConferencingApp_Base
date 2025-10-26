using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Entities.User
{
    public class RefreshToken : BaseEntity
    {
        public string Token { get; set; }
        public long UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string ReplacedByToken { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }

    }
}
