using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Infrastructure.Services.AuthServices
{
        public class CurrentUserService : ICurrentUserService
        {
            private readonly IHttpContextAccessor _httpContextAccessor;

            public CurrentUserService(IHttpContextAccessor httpContextAccessor)
            {
                _httpContextAccessor = httpContextAccessor;
            }

            private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

            public long? UserId
            {
                get
                {
                    var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User?.FindFirst("sub")?.Value
                        ?? User?.FindFirst("userId")?.Value;

                    return long.TryParse(userIdClaim, out var userId) ? userId : null;
                }
            }

            public string? Username =>
                User?.FindFirst(ClaimTypes.Name)?.Value
                ?? User?.FindFirst("username")?.Value
                ?? User?.FindFirst("preferred_username")?.Value;

            public string? Email =>
                User?.FindFirst(ClaimTypes.Email)?.Value
                ?? User?.FindFirst("email")?.Value;

            public string? Role =>
                User?.FindFirst(ClaimTypes.Role)?.Value
                ?? User?.FindFirst("role")?.Value;

            public IEnumerable<string> Roles =>
                User?.FindAll(ClaimTypes.Role).Select(c => c.Value)
                ?? User?.FindAll("role").Select(c => c.Value)
                ?? Enumerable.Empty<string>();

            public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

            public bool IsInRole(string role)
            {
                if (string.IsNullOrWhiteSpace(role))
                    return false;

                return Roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
            }

            public string? GetClaim(string claimType)
            {
                return User?.FindFirst(claimType)?.Value;
            }

            public IEnumerable<string> GetClaims(string claimType)
            {
                return User?.FindAll(claimType).Select(c => c.Value) ?? Enumerable.Empty<string>();
            }
        }
    
}
