using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using VideoConferencingApp.Application.Common.IAuthServices;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Application.Common.IEventHandlerServices;
using VideoConferencingApp.Application.DTOs.Authentication;
using VideoConferencingApp.Application.DTOs.UserDto;
using VideoConferencingApp.Domain.CacheKeys;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Events.Notification;
using VideoConferencingApp.Domain.Exceptions;
using VideoConferencingApp.Domain.Interfaces;

namespace VideoConferencingApp.Application.Services.UserServices
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _repo;
        private readonly IStaticCacheManager _cache;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<UserService> _logger;
        private readonly IFileStorageService _fileStorageService;
        private readonly IRepository<UserSession> _sessionRepo;
        private readonly IUserDeviceTokenService _deviceTokenService;
        private readonly IBCryptPasswordServices _bryptPasswordServices;

        public UserService(
            IRepository<User> repo,
            IStaticCacheManager cache,
            IEventPublisher eventPublisher,
            ILogger<UserService> logger,
            IFileStorageService fileStorageService,
            IRepository<UserSession> sessionRepo,
            IUserDeviceTokenService deviceTokenService,
            IBCryptPasswordServices bryptPasswordServices)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
            _sessionRepo = sessionRepo ?? throw new ArgumentNullException(nameof(sessionRepo));
            _deviceTokenService = deviceTokenService ?? throw new ArgumentNullException(nameof(deviceTokenService));
            _bryptPasswordServices = bryptPasswordServices ?? throw new ArgumentNullException(nameof(bryptPasswordServices));
        }


        #region Create

        /// <summary>
        /// Creates a new user with validation and caching
        /// </summary>
        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(user.Username))
                    throw new ArgumentException("Username is required.", nameof(user.Username));

                if (string.IsNullOrWhiteSpace(user.Email))
                    throw new ArgumentException("Email is required.", nameof(user.Email));

                // Check for duplicates
                var existingEmail = await GetByEmailAsync(user.Email);
                if (existingEmail != null)
                    throw new InvalidOperationException($"Email {user.Email} is already in use.");

                var existingUsername = await GetByUsernameAsync(user.Username);
                if (existingUsername != null)
                    throw new InvalidOperationException($"Username {user.Username} is already in use.");

                // Set defaults
                user.IsActive = true;
                user.CreatedOnUtc = DateTime.UtcNow;

                // Persist
                await _repo.InsertAsync(user);

                // Invalidate all user-related caches
                await _cache.RemoveByPrefixAsync(UserCacheKeys.PrefixRaw);

                // Prime cache for this user
                await _cache.SetAsync(UserCacheKeys.ById(user.Id), user);
                await _cache.SetAsync(UserCacheKeys.ByEmail(user.Email), user);
                await _cache.SetAsync(UserCacheKeys.ByUsername(user.Username), user);

               
                _logger.LogInformation("User created: {UserId} - {Username}", user.Id, user.Username);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user with email {Email}", user?.Email);
                throw;
            }
        }

        #endregion

        #region Update

        /// <summary>
        /// Updates an existing user with the provided data
        /// </summary>
        public async Task<User> UpdateUserAsync(User user)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                if (user.Id <= 0)
                    throw new ArgumentException("Invalid user ID.", nameof(user.Id));

                if (string.IsNullOrWhiteSpace(user.Username))
                    throw new ArgumentException("Username is required.", nameof(user.Username));

                if (string.IsNullOrWhiteSpace(user.Email))
                    throw new ArgumentException("Email is required.", nameof(user.Email));

                // Check if user exists
                var existingUser = await _repo.GetByIdAsync(user.Id);
                if (existingUser == null)
                    throw new InvalidOperationException($"User with ID {user.Id} not found.");

                // Track changes for event
                var changedFields = new Dictionary<string, object>();

                // Check for duplicate email if email has changed
                if (!string.Equals(existingUser.Email, user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var userWithEmail = await GetByEmailAsync(user.Email);
                    if (userWithEmail != null && userWithEmail.Id != user.Id)
                        throw new InvalidOperationException($"Email {user.Email} is already in use by another user.");

                    changedFields["Email"] = new { Old = existingUser.Email, New = user.Email };
                }

                // Check for duplicate username if username has changed
                if (!string.Equals(existingUser.Username, user.Username, StringComparison.OrdinalIgnoreCase))
                {
                    var userWithUsername = await GetByUsernameAsync(user.Username);
                    if (userWithUsername != null && userWithUsername.Id != user.Id)
                        throw new InvalidOperationException($"Username {user.Username} is already in use by another user.");

                    changedFields["Username"] = new { Old = existingUser.Username, New = user.Username };
                }

                // Track other changes
                if (existingUser.DisplayName != user.DisplayName)
                    changedFields["DisplayName"] = new { Old = existingUser.DisplayName, New = user.DisplayName };

                if (existingUser.ProfilePictureUrl != user.ProfilePictureUrl)
                    changedFields["ProfilePictureUrl"] = new { Old = existingUser.ProfilePictureUrl, New = user.ProfilePictureUrl };

                if (existingUser.Bio != user.Bio)
                    changedFields["Bio"] = new { Old = existingUser.Bio, New = user.Bio };

                // Set update timestamp
                user.UpdatedOnUtc = DateTime.UtcNow;

                // Update the user
                await _repo.UpdateAsync(user);

                // Invalidate all user-related caches
                await _cache.RemoveByPrefixAsync(UserCacheKeys.PrefixRaw);

                // Prime cache with updated user data
                await _cache.SetAsync(UserCacheKeys.ById(user.Id), user);
                await _cache.SetAsync(UserCacheKeys.ByEmail(user.Email), user);
                await _cache.SetAsync(UserCacheKeys.ByUsername(user.Username), user);

                
                _logger.LogInformation("User updated: {UserId} - {Username}", user.Id, user.Username);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", user?.Id);
                throw;
            }
        }

        /// <summary>
        /// Updates specific fields of an existing user
        /// </summary>
        public async Task<User> UpdateUserPartialAsync(long id, Action<User> updateAction)
        {
            try
            {
                if (id <= 0)
                    throw new ArgumentException("Invalid user ID.", nameof(id));

                if (updateAction == null)
                    throw new ArgumentNullException(nameof(updateAction));

                // Get existing user
                var user = await _repo.GetByIdAsync(id);
                if (user == null)
                    throw new InvalidOperationException($"User with ID {id} not found.");

                // Store original values for comparison
                var originalEmail = user.Email;
                var originalUsername = user.Username;
                var originalDisplayName = user.DisplayName;
                var originalProfilePicture = user.ProfilePictureUrl;
                var originalBio = user.Bio;

                // Apply updates
                updateAction(user);

                // Validate required fields after update
                if (string.IsNullOrWhiteSpace(user.Username))
                    throw new ArgumentException("Username cannot be empty.");

                if (string.IsNullOrWhiteSpace(user.Email))
                    throw new ArgumentException("Email cannot be empty.");

                // Track changes
                var changedFields = new Dictionary<string, object>();

                // Check for duplicate email if changed
                if (!string.Equals(originalEmail, user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var userWithEmail = await GetByEmailAsync(user.Email);
                    if (userWithEmail != null && userWithEmail.Id != id)
                        throw new InvalidOperationException($"Email {user.Email} is already in use by another user.");

                    changedFields["Email"] = new { Old = originalEmail, New = user.Email };
                }

                // Check for duplicate username if changed
                if (!string.Equals(originalUsername, user.Username, StringComparison.OrdinalIgnoreCase))
                {
                    var userWithUsername = await GetByUsernameAsync(user.Username);
                    if (userWithUsername != null && userWithUsername.Id != id)
                        throw new InvalidOperationException($"Username {user.Username} is already in use by another user.");

                    changedFields["Username"] = new { Old = originalUsername, New = user.Username };
                }

                // Track other changes
                if (originalDisplayName != user.DisplayName)
                    changedFields["DisplayName"] = new { Old = originalDisplayName, New = user.DisplayName };

                if (originalProfilePicture != user.ProfilePictureUrl)
                    changedFields["ProfilePictureUrl"] = new { Old = originalProfilePicture, New = user.ProfilePictureUrl };

                if (originalBio != user.Bio)
                    changedFields["Bio"] = new { Old = originalBio, New = user.Bio };

                // Set update timestamp
                user.UpdatedOnUtc = DateTime.UtcNow;

                // Update the user
                await _repo.UpdateAsync(user);

                // Invalidate all user-related caches
                await _cache.RemoveByPrefixAsync(UserCacheKeys.PrefixRaw);

                // Prime cache with updated user data
                await _cache.SetAsync(UserCacheKeys.ById(user.Id), user);
                await _cache.SetAsync(UserCacheKeys.ByEmail(user.Email), user);
                await _cache.SetAsync(UserCacheKeys.ByUsername(user.Username), user);

               
                _logger.LogInformation("User partially updated: {UserId} - {FieldCount} fields changed",
                    user.Id, changedFields.Count);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error partially updating user {UserId}", id);
                throw;
            }
        }

        #endregion

        #region Read

        /// <summary>
        /// Get user by ID with caching
        /// </summary>
        public Task<User?> GetByIdAsync(long id)
        {
            var key = UserCacheKeys.ById(id);
            return _cache.GetOrCreateAsync(
                key,
                async (ct) =>
                {
                    var user = await _repo.GetByIdAsync(id);
                    if (user != null && !user.IsDeleted)
                        return user;
                    return null;
                },
                ttl: _cache.GetDefaultTtl()
            );
        }

        /// <summary>
        /// Get user by email with caching
        /// </summary>
        public Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Task.FromResult<User?>(null);

            var key = UserCacheKeys.ByEmail(email);
            return _cache.GetOrCreateAsync(
                key,
                async (ct) =>
                {
                    var users = await _repo.FindAsync(u =>
                        u.Email.ToUpper() == email.ToUpper() &&
                        !u.IsDeleted);
                    return users.Count > 0 ? users[0] : null;
                },
                ttl: _cache.GetDefaultTtl()
            );
        }

        /// <summary>
        /// Get user by username with caching
        /// </summary>
        public Task<User?> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Task.FromResult<User?>(null);

            var key = UserCacheKeys.ByUsername(username);
            return _cache.GetOrCreateAsync(
                key,
                async (ct) =>
                {
                    var users = await _repo.FindAsync(u =>
                        u.Username.ToUpper() == username.ToUpper() &&
                        !u.IsDeleted);
                    return users.Count > 0 ? users[0] : null;
                },
                ttl: _cache.GetDefaultTtl()
            );
        }

        /// <summary>
        /// Get all users with caching
        /// </summary>
        public Task<IList<User>> GetAllAsync()
        {
            var key = UserCacheKeys.All;
            return _cache.GetOrCreateAsync<IList<User>>(
                key,
                async (ct) =>
                {
                    var users = await _repo.FindAsync(u => !u.IsDeleted);
                    return users.ToList();
                },
                ttl: _cache.GetLongTtl()
            );
        }

        /// <summary>
        /// Get all active users
        /// </summary>
        public Task<IList<User>> GetAllActiveAsync()
        {
            var key = UserCacheKeys.AllActive;
            return _cache.GetOrCreateAsync<IList<User>>(
                key,
                async (ct) =>
                {
                    var users = await _repo.FindAsync(u => u.IsActive && !u.IsDeleted);
                    return users.ToList();
                },
                ttl: _cache.GetDefaultTtl()
            );
        }

        /// <summary>
        /// Get online users
        /// </summary>
        public Task<IList<User>> GetOnlineUsersAsync()
        {
            var key = UserCacheKeys.OnlineUsers;
            return _cache.GetOrCreateAsync<IList<User>>(
                key,
                async (ct) =>
                {
                    var users = await _repo.FindAsync(u =>
                        u.IsOnline &&
                        u.IsActive &&
                        !u.IsDeleted);
                    return users.ToList();
                },
                ttl: _cache.GetShortTtl() // Shorter TTL for real-time data
            );
        }

        /// <summary>
        /// Get users by role
        /// </summary>
        public Task<IList<User>> GetByRoleAsync(UserRole role)
        {
            var key = UserCacheKeys.ByRole(role);
            return _cache.GetOrCreateAsync<IList<User>>(
                key,
                async (ct) =>
                {
                    var users = await _repo.FindAsync(u =>
                        u.Role == role &&
                        u.IsActive &&
                        !u.IsDeleted);
                    return users.ToList();
                },
                ttl: _cache.GetDefaultTtl()
            );
        }

        /// <summary>
        /// Get total user count
        /// </summary>
        public Task<int> GetUserCountAsync()
        {
            var key = UserCacheKeys.Count;
            return _cache.GetOrCreateAsync(
                key,
                async (ct) =>
                {
                    var users = await _repo.FindAsync(u => !u.IsDeleted);
                    return users.Count;
                },
                ttl: _cache.GetDefaultTtl()
            );
        }

        #endregion

        #region Delete/Deactivate

        /// <summary>
        /// Deactivate user
        /// </summary>
        public async Task<bool> DeactivateUserAsync(long id, string reason = null)
        {
            try
            {
                var user = await _repo.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Attempted to deactivate non-existent user {UserId}", id);
                    return false;
                }

                if (!user.IsActive)
                {
                    _logger.LogInformation("User {UserId} is already deactivated", id);
                    return true;
                }

                user.IsActive = false;
                user.UpdatedOnUtc = DateTime.UtcNow;
                await _repo.UpdateAsync(user);

                // Invalidate all user-related caches
                await _cache.RemoveByPrefixAsync(UserCacheKeys.PrefixRaw);

                // Prime cache with updated user
                await _cache.SetAsync(UserCacheKeys.ById(user.Id), user);

                // Send notification to user
                await PublishUserDeactivatedNotificationAsync(user, reason);

                _logger.LogInformation("User deactivated: {UserId} - {Username}", user.Id, user.Username);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Soft delete user
        /// </summary>
        public async Task<bool> SoftDeleteUserAsync(long id, long? deletedBy = null)
        {
            try
            {
                var user = await _repo.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("Attempted to delete non-existent user {UserId}", id);
                    return false;
                }

                if (user.IsDeleted)
                {
                    _logger.LogInformation("User {UserId} is already deleted", id);
                    return true;
                }

                user.IsActive = false;
                user.IsDeleted = true; // Fixed: was false in your original code
                user.UpdatedOnUtc = DateTime.UtcNow;

                await _repo.UpdateAsync(user);

                // Invalidate all user-related caches
                await _cache.RemoveByPrefixAsync(UserCacheKeys.PrefixRaw);

                // Prime cache with updated user
                await _cache.SetAsync(UserCacheKeys.ById(user.Id), user);

              
                // Send notification to user
                await PublishUserDeletedNotificationAsync(user);

                _logger.LogInformation("User soft deleted: {UserId} - {Username} by {DeletedBy}",
                    user.Id, user.Username, deletedBy);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting user {UserId}", id);
                throw;
            }
        }

        #endregion

        #region Profile Management

        /// <summary>
        /// Get user profile with additional information
        /// </summary>
        public async Task<UserProfileDto> GetUserProfileAsync(long userId, long requesterId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null || user.IsDeleted)
                    throw new NotFoundException($"User with ID {userId} not found.");

                var profile = new UserProfileDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    PhoneNumber = user.PhoneNumber,
                    Bio = user.Bio,
                    IsOnline = user.IsOnline,
                    LastSeen = user.LastSeen,
                    EmailVerified = user.EmailVerified,
                    EmailVerifiedAt = user.EmailVerifiedAt,
                    CreatedAt = user.CreatedOnUtc,
                    Role = user.Role
                };

                // If requesting own profile, return full details
                if (userId == requesterId)
                {
                    return profile;
                }

                // For other users, check contact status and privacy settings
                // This would integrate with your ContactService
                // profile.IsContact = await _contactService.AreContactsAsync(requesterId, userId);
                // profile.IsBlocked = await _contactService.IsBlockedAsync(requesterId, userId);
                // etc.

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile for {UserId} requested by {RequesterId}",
                    userId, requesterId);
                throw;
            }
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        public async Task<UserProfileDto> UpdateProfileAsync(long userId, UpdateProfileDto dto)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null || user.IsDeleted)
                    throw new NotFoundException($"User with ID {userId} not found.");

                // Validate username if changed
                if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != user.Username)
                {
                    if (!await IsUsernameAvailableAsync(dto.Username))
                        throw new ValidationException($"Username {dto.Username} is already taken.");

                    user.Username = dto.Username;
                }

                // Update other fields
                if (!string.IsNullOrWhiteSpace(dto.DisplayName))
                    user.DisplayName = dto.DisplayName;

                if (dto.PhoneNumber != null)
                    user.PhoneNumber = dto.PhoneNumber;

                if (dto.Bio != null)
                    user.Bio = dto.Bio;

                await UpdateUserAsync(user);

                return await GetUserProfileAsync(userId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Update user profile picture
        /// </summary>
        public async Task<string> UpdateProfilePictureAsync(long userId, IFormFile file)
        {
            try
            {
                
                var user = await GetByIdAsync(userId);
                if (user == null || user.IsDeleted)
                    throw new NotFoundException($"User with ID {userId} not found.");

                // Delete old profile picture if exists
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
                {
                    await _fileStorageService.DeleteFileAsync(user.ProfilePictureUrl);
                }

                // Upload new profile picture
                var fileName = $"profile_{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = await _fileStorageService.SaveFileAsync(file.OpenReadStream(), fileName, file.ContentType);

                user.ProfilePictureUrl = filePath;
                await UpdateUserAsync(user);

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile picture for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Delete user profile picture
        /// </summary>
        public async Task<bool> DeleteProfilePictureAsync(long userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null || user.IsDeleted)
                    throw new NotFoundException($"User with ID {userId} not found.");

                if (string.IsNullOrEmpty(user.ProfilePictureUrl))
                    return false;

                await _fileStorageService.DeleteFileAsync(user.ProfilePictureUrl);
                user.ProfilePictureUrl = null;
                await UpdateUserAsync(user);

               
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting profile picture for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get user security settings
        /// </summary>
        public async Task<UserSecuritySettingsDto> GetSecuritySettingsAsync(long userId)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null || user.IsDeleted)
                    throw new NotFoundException($"User with ID {userId} not found.");

                var devices = await _deviceTokenService.GetUserDevicesAsync(userId, activeOnly: true);

                return new UserSecuritySettingsDto
                {
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    TwoFactorEnabledAt = user.TwoFactorEnabledAt,
                    EmailVerified = user.EmailVerified,
                    EmailVerifiedAt = user.EmailVerifiedAt,
                    LastPasswordChangeAt = user.LastPasswordChangeAt,
                    LastLoginDate = user.LastLoginDate,
                    LastLoginIp = user.LastLoginIp,
                    LockoutEnabled = user.LockoutEnabled,
                    LockoutEnd = user.LockoutEnd,
                    AccessFailedCount = user.AccessFailedCount,
                    ActiveDevices = devices.Select(d => new UserDeviceDto
                    {
                        Id = d.Id,
                        DeviceName = d.DeviceName,
                        DeviceModel = d.DeviceModel,
                        Platform = d.Platform,
                        OsVersion = d.OsVersion,
                        RegisteredAt = d.CreatedOnUtc,
                        LastUsedAt = d.LastUsedAt,
                        IsCurrentDevice = false // Set by controller based on current request
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting security settings for user {UserId}", userId);
                throw;
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Check if username is available
        /// </summary>
        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return false;

                var existingUser = await GetByUsernameAsync(username);
                return existingUser == null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username availability for {Username}", username);
                throw;
            }
        }

        /// <summary>
        /// Check if email is available
        /// </summary>
        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                var existingUser = await GetByEmailAsync(email);
                return existingUser == null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email availability for {Email}", email);
                throw;
            }
        }

        #endregion


        #region Account Management

     
        /// <summary>
        /// Delete user account
        /// </summary>
        public async Task<bool> DeleteAccountAsync(long userId, string password, string reason = null)
        {
            try
            {
                var user = await GetByIdAsync(userId);
                if (user == null || user.IsDeleted)
                    throw new NotFoundException($"User with ID {userId} not found.");

                // Verify password
                if (!_bryptPasswordServices.VerifyPassword(password, user.PasswordHash))
                    throw new UnauthorizedException("Invalid password.");

                // Perform soft delete
                await SoftDeleteUserAsync(userId, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account for user {UserId}", userId);
                throw;
            }
        }

        #endregion


        #region Notification Event Publishers

        /// <summary>
        /// Publish user deactivated notification
        /// </summary>
        private async Task PublishUserDeactivatedNotificationAsync(User user, string reason)
        {
            try
            {
                await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
                {
                    EventId = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    UserId = user.Id,
                    To = user.Email,
                    Subject = "Your Account Has Been Deactivated",
                    TemplateName = "AccountDeactivated",
                    TemplateData = new Dictionary<string, string>
                    {
                        { "Username", user.Username },
                        { "DisplayName", user.DisplayName ?? user.Username },
                        { "DeactivationDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") },
                        { "Reason", reason ?? "No reason provided" },
                        { "SupportUrl", "https://yourapp.com/support" }
                    }
                });

                _logger.LogInformation("Deactivation notification sent to user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send deactivation notification to user {UserId}", user.Id);
            }
        }

        /// <summary>
        /// Publish user deleted notification
        /// </summary>
        private async Task PublishUserDeletedNotificationAsync(User user)
        {
            try
            {
                await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
                {
                    EventId = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    UserId = user.Id,
                    To = user.Email,
                    Subject = "Your Account Has Been Deleted",
                    TemplateName = "AccountDeleted",
                    TemplateData = new Dictionary<string, string>
                    {
                        { "Username", user.Username },
                        { "DisplayName", user.DisplayName ?? user.Username },
                        { "DeletionDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") },
                        { "SupportUrl", "https://yourapp.com/support" }
                    }
                });

                _logger.LogInformation("Deletion notification sent to user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send deletion notification to user {UserId}", user.Id);
            }
        }

        #endregion
    }
}