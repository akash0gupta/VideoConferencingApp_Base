using LinqToDB;
using Microsoft.Extensions.Logging;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Application.DTOs.Presence;
using VideoConferencingApp.Application.Services.MessagingServices;
using VideoConferencingApp.Domain.Entities.PresenceEntities;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Interfaces;

namespace VideoConferencingApp.Application.Services.MessagingServices
{
    public class ConnectionManagerService : IConnectionManagerService
    {
        private readonly IRepository<UserConnection> _connectionRepository;
        private readonly IRepository<UserPresence> _presenceRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ConnectionManagerService> _logger;

        public ConnectionManagerService(
            IRepository<UserConnection> connectionRepository,
            IRepository<UserPresence> presenceRepository,
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            ILogger<ConnectionManagerService> logger)
        {
            _connectionRepository = connectionRepository;
            _presenceRepository = presenceRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task AddConnectionAsync(
            string userId,
            string connectionId,
            string? deviceId,
            string? userAgent,
            string? ipAddress)
        {
            try
            {
                var connection = new UserConnection
                {
                    UserId = userId,
                    ConnectionId = connectionId,
                    DeviceId = deviceId,
                    DeviceName = ExtractDeviceName(userAgent),
                    UserAgent = userAgent,
                    IpAddress = ipAddress,
                    ConnectedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _connectionRepository.InsertAsync(connection);

                // Update user's online status
                await UpdatePresenceStatusAsync(userId, UserPresenceStatus.Online);

                _logger.LogInformation(
                    "Connection added - UserId: {UserId}, ConnectionId: {ConnectionId}, Device: {Device}",
                    userId, connectionId, deviceId ?? "Unknown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding connection for user {UserId}", userId);
                throw;
            }
        }

        public async Task RemoveConnectionAsync(string connectionId)
        {
            try
            {
                // ✅ Using .Table instead of custom method
                var connection = await _connectionRepository.Table
                    .FirstOrDefaultAsync(c => c.ConnectionId == connectionId && !c.IsDeleted);

                if (connection == null)
                {
                    _logger.LogWarning("Attempted to remove non-existent connection: {ConnectionId}", connectionId);
                    return;
                }

                var userId = connection.UserId;

                connection.IsDeleted = true;
                connection.UpdatedOnUtc = DateTime.UtcNow;

                await _connectionRepository.UpdateAsync(connection);


                var remainingConnections = await _connectionRepository.Table
                    .Where(c => c.UserId == userId && !c.IsDeleted)
                    .ToListAsync();

                if (!remainingConnections.Any())
                {

                    await UpdatePresenceStatusAsync(userId, UserPresenceStatus.Offline);

                    _logger.LogInformation("User {UserId} is now offline (no active connections)", userId);
                }

                _logger.LogInformation(
                    "Connection removed - UserId: {UserId}, ConnectionId: {ConnectionId}, RemainingConnections: {Count}",
                    userId, connectionId, remainingConnections.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing connection {ConnectionId}", connectionId);
                throw;
            }
        }
        public async Task<IList<string>> GetUserConnectionsAsync(string userId)
        {
            try
            {
                var connections = await _connectionRepository.Table
                    .Where(c => c.UserId == userId && !c.IsDeleted)
                    .Select(c => c.ConnectionId)
                    .ToListAsync();

                return connections;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connections for user {UserId}", userId);
                throw;
            }
        }

        public async Task<string?> GetUserIdByConnectionAsync(string connectionId)
        {
            try
            {
                var connection = await _connectionRepository.Table
                    .Where(c => c.ConnectionId == connectionId && !c.IsDeleted)
                    .Select(c => c.UserId)
                    .FirstOrDefaultAsync();

                return connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ID for connection {ConnectionId}", connectionId);
                throw;
            }
        }

        public async Task<bool> IsUserOnlineAsync(string userId)
        {
            try
            {
                var hasActiveConnection = await _connectionRepository.Table
                    .AnyAsync(c => c.UserId == userId && !c.IsDeleted);

                return hasActiveConnection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking online status for user {UserId}", userId);
                throw;
            }
        }

        public async Task UpdateLastActivityAsync(string connectionId)
        {
            try
            {
                var connection = await _connectionRepository.Table
                    .FirstOrDefaultAsync(c => c.ConnectionId == connectionId && !c.IsDeleted);

                if (connection != null)
                {
                    connection.LastActivityAt = DateTime.UtcNow;
                    await _connectionRepository.UpdateAsync(connection);

                    _logger.LogTrace("Last activity updated for connection {ConnectionId}", connectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last activity for connection {ConnectionId}", connectionId);
                // Don't throw - this is not critical
            }
        }

        #region Helper Methods

        private async Task UpdatePresenceStatusAsync(string userId, UserPresenceStatus status)
        {
            try
            {
                var presence = await _presenceRepository.Table
                    .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);

                if (presence == null)
                {
                    // Create new presence record
                    presence = new UserPresence
                    {
                        UserId = userId,
                        Status = status,
                        LastSeen = DateTime.UtcNow,
                        StatusChangedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _presenceRepository.InsertAsync(presence);
                }
                else
                {
                    // Update existing presence
                    var statusChanged = presence.Status != status;

                    presence.Status = status;
                    presence.LastSeen = DateTime.UtcNow;

                    if (statusChanged)
                    {
                        presence.StatusChangedAt = DateTime.UtcNow;
                    }

                    await _presenceRepository.UpdateAsync(presence);
                }

            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating presence status for user {UserId}", userId);
                // Don't throw - presence update is not critical
            }
        }


        private string? ExtractDeviceName(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return null;

            // Simple device name extraction from user agent
            if (userAgent.Contains("iPhone"))
                return "iPhone";
            if (userAgent.Contains("iPad"))
                return "iPad";
            if (userAgent.Contains("Android"))
                return "Android Device";
            if (userAgent.Contains("Windows"))
                return "Windows PC";
            if (userAgent.Contains("Macintosh"))
                return "Mac";
            if (userAgent.Contains("Linux"))
                return "Linux PC";

            return "Unknown Device";
        }

        #endregion
    }
}