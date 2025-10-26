using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.IRealTimeServices;

namespace VideoConferencingApp.Infrastructure.Services
{
    public class CallAuthorizationRequirement : IAuthorizationRequirement
    {
        public string TargetUserId { get; }

        public CallAuthorizationRequirement(string targetUserId)
        {
            TargetUserId = targetUserId;
        }
    }

    public class CallAuthorizationHandler : AuthorizationHandler<CallAuthorizationRequirement>
    {
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<CallAuthorizationHandler> _logger;

        public CallAuthorizationHandler(
            IConnectionManager connectionManager,
            ILogger<CallAuthorizationHandler> logger)
        {
            _connectionManager = connectionManager;
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            CallAuthorizationRequirement requirement)
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Authorization failed: User ID not found in claims");
                return Task.CompletedTask;
            }

            // Check if user is trying to call themselves
            if (userId == requirement.TargetUserId)
            {
                _logger.LogWarning("User {UserId} attempted to call themselves", userId);
                return Task.CompletedTask;
            }

            // Check if target user exists and is available
            if (!_connectionManager.IsUserAvailable(requirement.TargetUserId))
            {
                _logger.LogWarning(
                    "Authorization failed: Target user {TargetUserId} is not available",
                    requirement.TargetUserId
                );
                return Task.CompletedTask;
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
