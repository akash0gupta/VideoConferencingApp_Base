using VideoConferencingApp.Application.DTOs.Presence;

namespace VideoConferencingApp.Application.Services.MessagingServices
{
    public interface IPresenceService
    {
        Task UpdatePresenceAsync(string userId, UpdatePresenceDto dto);
        Task<UserPresenceDto?> GetUserPresenceAsync(string userId);
        Task<IList<UserPresenceDto>> GetOnlineUsersAsync();
        Task UpdateLastSeenAsync(string userId);
    }
}
