using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.DTOs.Notification;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.Services.UserServices
{
    public interface IUserDeviceTokenService
    {
        Task<UserDeviceToken> RegisterDeviceAsync(long userId, DeviceInfo deviceInfo);
        Task<bool> UnregisterDeviceAsync(string deviceToken);
        Task<bool> DeactivateDeviceAsync(string deviceToken);
        Task<UserDeviceToken> GetByIdAsync(long id);
        Task<UserDeviceToken> GetByDeviceTokenAsync(string deviceToken);
        Task<IList<UserDeviceToken>> GetUserDevicesAsync(long userId, bool activeOnly = true);
        Task<IList<UserDeviceToken>> GetUserDevicesByPlatformAsync(long userId, DevicePlatform platform);
        Task<bool> UpdateLastUsedAsync(string deviceToken);
        Task<int> CleanupInactiveDevicesAsync(int daysInactive = 90);
    }
}
