using LinqToDB;
using Mapster;
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
    public class PresenceService : IPresenceService
    {
        private readonly IRepository<UserPresence> _presenceRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PresenceService> _logger;

      public PresenceService(
           IRepository<UserPresence> presenceRepository,
           IRepository<User> userRepository,
          IUnitOfWork unitOfWork,
          ILogger<PresenceService> logger)
        {
            _presenceRepository = presenceRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task UpdatePresenceAsync(string userId, UpdatePresenceDto dto)
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
                        Status = dto.Status,
                        CustomMessage = dto.CustomMessage,
                        LastSeen = DateTime.UtcNow,
                        StatusChangedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _presenceRepository.InsertAsync(presence);
                }
                else
                {
                    // Update existing presence
                    var statusChanged = presence.Status != dto.Status;

                    presence.Status = dto.Status;
                    presence.CustomMessage = dto.CustomMessage;
                    presence.LastSeen = DateTime.UtcNow;

                    if (statusChanged)
                    {
                        presence.StatusChangedAt = DateTime.UtcNow;
                    }

                    await _presenceRepository.UpdateAsync(presence);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogDebug("Presence updated for user {UserId} to {Status}", userId, dto.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating presence for user {UserId}", userId);
                throw;
            }
        }


        public async Task<UserPresenceDto?> GetUserPresenceAsync(string userId)
        {
            try
            {
                // ✅ Using .Table with LINQ query
                var presence = await _presenceRepository.Table
                    .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);

                if (presence == null)
                {
                    // Return default offline status if no record exists
                    var user = await _userRepository.GetByIdAsync(long.Parse(userId));
                    if (user == null)
                        return null;

                    return new UserPresenceDto
                    {
                        UserId = userId,
                        Username = user.Username,
                        Status = UserPresenceStatus.Offline,
                        CustomMessage = null,
                        LastSeen = user.LastSeen,
                        IsOnline = false
                    };
                }

                var userEntity = await _userRepository.GetByIdAsync(long.Parse(userId));

                return new UserPresenceDto
                {
                    UserId = userId,
                    Username = userEntity?.Username ?? "Unknown",
                    Status = presence.Status,
                    CustomMessage = presence.CustomMessage,
                    LastSeen = presence.LastSeen,
                    IsOnline = presence.Status == UserPresenceStatus.Online
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting presence for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IList<UserPresenceDto>> GetOnlineUsersAsync()
        {
            try
            {
                // ✅ Using .Table with LINQ query and JOIN
                var onlinePresences = await (
                    from presence in _presenceRepository.Table
                    join user in _userRepository.Table on presence.UserId equals user.Id.ToString()
                    where presence.Status == UserPresenceStatus.Online &&
                          !presence.IsDeleted &&
                          user.IsActive &&
                          !user.IsDeleted
                    select new UserPresenceDto
                    {
                        UserId = presence.UserId,
                        Username = user.Username,
                        Status = presence.Status,
                        CustomMessage = presence.CustomMessage,
                        LastSeen = presence.LastSeen,
                        IsOnline = true
                    }
                ).ToListAsync();

                _logger.LogDebug("Retrieved {Count} online users", onlinePresences.Count);

                return onlinePresences;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting online users");
                throw;
            }
        }
        public async Task UpdateLastSeenAsync(string userId)
        {
            try
            {
                // ✅ Using .Table with LINQ query
                var presence = await _presenceRepository.Table
                    .FirstOrDefaultAsync(p => p.UserId == userId && !p.IsDeleted);

                if (presence == null)
                {
                    presence = new UserPresence
                    {
                        UserId = userId,
                        Status = UserPresenceStatus.Online,
                        LastSeen = DateTime.UtcNow,
                        StatusChangedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _presenceRepository.InsertAsync(presence);
                }
                else
                {
                    presence.LastSeen = DateTime.UtcNow;
                    await _presenceRepository.UpdateAsync(presence);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogTrace("Last seen updated for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last seen for user {UserId}", userId);
                throw;
            }
        }
    }
}