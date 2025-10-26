using Microsoft.AspNetCore.SignalR; // <-- CRITICAL: This using statement must be present.
using VideoConferencingApp.Application.Interfaces.Common.INotificationServices;

namespace VideoConferencingApp.Infrastructure.RealTime
{
    public class SignalRNotificationService : INotificationService
    {
        private readonly IHubContext<AppHub> _hubContext;

        // The constructor now takes IHubContext<AppHub>.
        public SignalRNotificationService(IHubContext<AppHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task NotifyUserAsync(string userId, string method, object payload)
        {
            // The IHubContext gives you access to the 'Clients' property.
            return _hubContext.Clients.Group($"user_{userId}").SendAsync(method, payload);
        }

        public Task NotifyGroupAsync(string groupName, string method, object payload)
        {
            return _hubContext.Clients.Group(groupName).SendAsync(method, payload);
        }

        public Task NotifyAllAsync(string method, object payload)
        {
            return _hubContext.Clients.All.SendAsync(method, payload);
        }
    }
}