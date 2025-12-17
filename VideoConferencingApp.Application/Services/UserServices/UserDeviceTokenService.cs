using LinqToDB;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Application.Common.IEventHandlerServices;
using VideoConferencingApp.Domain.CacheKeys;
using VideoConferencingApp.Application.DTOs.Notification;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Interfaces;

namespace VideoConferencingApp.Application.Services.UserServices
{
    public class UserDeviceTokenService : IUserDeviceTokenService
    {
        private readonly IRepository<UserDeviceToken> _repo;
        private readonly IStaticCacheManager _cache;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<UserDeviceTokenService> _logger;

        public UserDeviceTokenService(
            IRepository<UserDeviceToken> repo,
            IStaticCacheManager cache,
            IEventPublisher eventPublisher,
            ILogger<UserDeviceTokenService> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserDeviceToken> RegisterDeviceAsync(long userId, DeviceInfo deviceInfo)
        {
            try
            {
                if (userId <= 0)
                    throw new ArgumentException("Invalid user ID", nameof(userId));

                if (deviceInfo == null)
                    throw new ArgumentNullException(nameof(deviceInfo));

                if (string.IsNullOrWhiteSpace(deviceInfo.DeviceToken))
                    throw new ArgumentException("Device token is required", nameof(deviceInfo.DeviceToken));

                // Check if device token already exists
                var existingToken = await _repo.Table.FirstOrDefaultAsync(c => c.DeviceToken == deviceInfo.DeviceToken); 

                if (existingToken != null)
                {
                    // Update existing token
                    existingToken.UserId = userId;
                    existingToken.Platform = deviceInfo.Platform;
                    existingToken.DeviceId = deviceInfo.DeviceId;
                    existingToken.DeviceName = deviceInfo.DeviceName;
                    existingToken.DeviceModel = deviceInfo.DeviceModel;
                    existingToken.OsVersion = deviceInfo.OsVersion;
                    existingToken.AppVersion = deviceInfo.AppVersion;
                    existingToken.IsActive = true;
                    existingToken.IsDeleted = false;
                    existingToken.UpdatedOnUtc = DateTime.UtcNow;
                    existingToken.LastUsedAt = DateTime.UtcNow;

                    await _repo.UpdateAsync(existingToken);

                    _logger.LogInformation(
                        "Updated existing device token for user {UserId}, platform {Platform}",
                        userId, deviceInfo.Platform);

                    // Invalidate caches
                    await InvalidateDeviceTokenCachesAsync(existingToken);

                    return existingToken;
                }

                // Create new token
                var deviceToken = new UserDeviceToken
                {
                    UserId = userId,
                    DeviceToken = deviceInfo.DeviceToken,
                    Platform = deviceInfo.Platform,
                    DeviceId = deviceInfo.DeviceId,
                    DeviceName = deviceInfo.DeviceName,
                    DeviceModel = deviceInfo.DeviceModel,
                    OsVersion = deviceInfo.OsVersion,
                    AppVersion = deviceInfo.AppVersion,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    LastUsedAt = DateTime.UtcNow
                };

                await _repo.InsertAsync(deviceToken);

                _logger.LogInformation(
                    "Registered new device token for user {UserId}, platform {Platform}",
                    userId, deviceInfo.Platform);

                // Invalidate caches
                await InvalidateDeviceTokenCachesAsync(deviceToken);

               

                return deviceToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> UnregisterDeviceAsync(string deviceToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceToken))
                    throw new ArgumentException("Device token is required", nameof(deviceToken));

                var device = await _repo.Table.FirstOrDefaultAsync(c => c.DeviceToken == deviceToken);
                if (device == null)
                {
                    _logger.LogWarning("Attempted to unregister non-existent device token");
                    return false;
                }

                device.IsDeleted = true;
                device.IsActive = false;
                device.UpdatedOnUtc = DateTime.UtcNow;

                await _repo.UpdateAsync(device);

                _logger.LogInformation(
                    "Unregistered device token for user {UserId}",
                    device.UserId);

                // Invalidate caches
                await InvalidateDeviceTokenCachesAsync(device);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering device token");
                throw;
            }
        }

        public async Task<bool> DeactivateDeviceAsync(string deviceToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceToken))
                    throw new ArgumentException("Device token is required", nameof(deviceToken));

                var device = await _repo.Table.FirstOrDefaultAsync(c => c.DeviceToken == deviceToken); ;
                if (device == null)
                {
                    _logger.LogWarning("Attempted to deactivate non-existent device token");
                    return false;
                }

                if (!device.IsActive)
                {
                    _logger.LogInformation("Device token already deactivated");
                    return true;
                }

                device.IsActive = false;
                device.UpdatedOnUtc = DateTime.UtcNow;

                await _repo.UpdateAsync(device);

                _logger.LogInformation(
                    "Deactivated device token for user {UserId}",
                    device.UserId);

                // Invalidate caches
                await InvalidateDeviceTokenCachesAsync(device);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating device token");
                throw;
            }
        }

        public Task<UserDeviceToken> GetByIdAsync(long id)
        {
            var key = UserDeviceTokenCacheKeys.ById(id);
            return _cache.GetOrCreateAsync<UserDeviceToken>(
                key,
                async (ct) =>
                {
                    var device = await _repo.GetByIdAsync(id);
                    if (device != null && !device.IsDeleted)
                        return device;
                    return null;
                },
                ttl: _cache.GetDefaultTtl()
            );
        }

        public Task<UserDeviceToken> GetByDeviceTokenAsync(string deviceToken)
        {
            if (string.IsNullOrWhiteSpace(deviceToken))
                return Task.FromResult<UserDeviceToken>(null);

            var key = UserDeviceTokenCacheKeys.ByDeviceToken(deviceToken);
            return _cache.GetOrCreateAsync<UserDeviceToken>(
                key,
                async (ct) =>
                {
                    var device = await _repo.Table.FirstOrDefaultAsync(c => c.DeviceToken == deviceToken);
                    if (device != null && !device.IsDeleted)
                        return device;
                    return null;
                },
                ttl: _cache.GetDefaultTtl()
            );
        }

        public Task<IList<UserDeviceToken>> GetUserDevicesAsync(long userId, bool activeOnly = true)
        {
            var key = activeOnly
                ? UserDeviceTokenCacheKeys.ActiveByUserId(userId)
                : UserDeviceTokenCacheKeys.ByUserId(userId);

            return _cache.GetOrCreateAsync<IList<UserDeviceToken>>(
                key,
                async (ct) =>
                {
                    var devices =  _repo.Table.Where(c => c.UserId == userId && c.IsActive== activeOnly);
                    return devices.OrderByDescending(d => d.LastUsedAt ?? d.CreatedOnUtc).ToList();
                },
                ttl: _cache.GetDefaultTtl()
            );
        }

        public Task<IList<UserDeviceToken>> GetUserDevicesByPlatformAsync(long userId, DevicePlatform platform)
        {
            var key = UserDeviceTokenCacheKeys.ByUserIdAndPlatform(userId, platform);
            return _cache.GetOrCreateAsync<IList<UserDeviceToken>>(
                key,
                async (ct) =>
                {
                    var devices =  _repo.Table.Where(x=> x.UserId==userId && x.Platform==platform);
                    return devices.OrderByDescending(d => d.LastUsedAt ?? d.CreatedOnUtc).ToList();
                },
                ttl: _cache.GetDefaultTtl()
            );
        }

        public async Task<bool> UpdateLastUsedAsync(string deviceToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deviceToken))
                    return false;

                var token = await _repo.Table.FirstOrDefaultAsync(t => t.DeviceToken == deviceToken);

                if (token != null)
                {
                    token.LastUsedAt = DateTime.UtcNow;
                    await _repo.UpdateAsync(token);
                    await _cache.RemoveAsync(UserDeviceTokenCacheKeys.ByDeviceToken(deviceToken));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last used timestamp for device token");
                return false;
            }
        }

        public async Task<int> CleanupInactiveDevicesAsync(int daysInactive = 90)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysInactive);

                var count = await _repo.Table.Where(t => (t.LastUsedAt ?? t.CreatedOnUtc) < cutoffDate || !t.IsActive)
            .Set(t => t.IsDeleted, true)
            .Set(t => t.IsActive, false)
            .Set(t => t.UpdatedOnUtc, DateTime.UtcNow)
            .UpdateAsync();               
                _logger.LogInformation(
                    "Cleaned up {Count} inactive device tokens (inactive for {Days} days)",
                    count, daysInactive);

                // Invalidate all device token caches
                await _cache.RemoveByPrefixAsync(UserDeviceTokenCacheKeys.PrefixRaw);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up inactive device tokens");
                throw;
            }
        }

        private async Task InvalidateDeviceTokenCachesAsync(UserDeviceToken device)
        {
            await _cache.RemoveByPrefixAsync(UserDeviceTokenCacheKeys.PrefixRaw);
        }
    }
}
