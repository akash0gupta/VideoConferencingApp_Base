using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Infrastructure.Services.AuthServices
{

    public interface ICurrentUserService
    {
        long? UserId { get; }
        string? Username { get; }
        string? Email { get; }
        string? Role { get; }
        IEnumerable<string> Roles { get; }
        bool IsAuthenticated { get; }
        bool IsInRole(string role);
        string? GetClaim(string claimType);
        IEnumerable<string> GetClaims(string claimType);
    }
}
