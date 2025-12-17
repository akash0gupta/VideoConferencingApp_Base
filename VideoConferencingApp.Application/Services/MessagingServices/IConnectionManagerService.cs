namespace VideoConferencingApp.Application.Services.MessagingServices
{
    public interface IConnectionManagerService
    {
        Task AddConnectionAsync(string userId, string connectionId, string? deviceId, string? userAgent, string? ipAddress);
        Task RemoveConnectionAsync(string connectionId);
        Task<IList<string>> GetUserConnectionsAsync(string userId);
        Task<string?> GetUserIdByConnectionAsync(string connectionId);
        Task<bool> IsUserOnlineAsync(string userId);
        Task UpdateLastActivityAsync(string connectionId);
    }
}
