using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using VideoConferencingApp.Application.Interfaces.Common.IRealTimeServices;

namespace VideoConferencingApp.Infrastructure.RealTime
{
    [Authorize]
    public class AppHub : Hub<ICallClient>
    {
        private readonly IPresenceService _presenceService;
        private readonly ILogger<AppHub> _logger;

        // Inject the new presence service
        public AppHub(IPresenceService presenceService, ILogger<AppHub> logger)
        {
            _presenceService = presenceService;
            _logger = logger;
        }

        /// <summary>
        /// Called automatically by SignalR when a client connects.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            // Get the user's ID from their JWT token.
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
            {
                // Call our service to mark the user as online.
                await _presenceService.UserConnectedAsync(userId, Context.ConnectionId);
                _logger.LogInformation("User {UserId} connected with ConnectionId {ConnectionId}", userId, Context.ConnectionId);

                // You could also publish a UserStatusChangedEvent here to notify contacts.
            }
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called automatically by SignalR when a client disconnects.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
            {
                // Call our service to mark the user as offline.
                await _presenceService.UserDisconnectedAsync(userId, Context.ConnectionId);
                _logger.LogInformation("User {UserId} disconnected with ConnectionId {ConnectionId}", userId, Context.ConnectionId);

                // You could publish another UserStatusChangedEvent here.
            }

            if (exception != null)
            {
                _logger.LogWarning(exception, "A client disconnected with an error.");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
