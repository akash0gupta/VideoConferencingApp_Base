using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoConferencingApp.Api.DTOs;
using VideoConferencingApp.Api.DTOs.ContactDto;
using VideoConferencingApp.API.Controllers.Base;
using VideoConferencingApp.Application.Common.IAuthServices;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Application.DTOs.Authentication;
using VideoConferencingApp.Application.DTOs.UserDto;
using VideoConferencingApp.Application.Events;
using VideoConferencingApp.Application.Services.ContactServices;
using VideoConferencingApp.Application.Services.UserServices;
using VideoConferencingApp.Domain.Entities;
using VideoConferencingApp.Domain.Exceptions;
using VideoConferencingApp.Domain.Interfaces;
using VideoConferencingApp.Infrastructure.Services.AuthServices;


namespace VideoConferencingApp.API.Controllers
{
    public class UsersController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly IUserDeviceTokenService _deviceTokenService;
        private readonly IContactService _contactService;
        private readonly IAuthService _authService;

        public UsersController(
            IUserService userService,
            IMapper mapper,
            IAuthService authService,
            IUserDeviceTokenService deviceTokenService,
            IContactService contactService,
            ICurrentUserService currentUserService,
            IHttpContextService httpContextService,
            IResponseHeaderService responseHeaderService,
            ILogger<UsersController> logger)
            : base(logger, currentUserService, httpContextService, responseHeaderService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _deviceTokenService = deviceTokenService ?? throw new ArgumentNullException(nameof(deviceTokenService));
            _contactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
        }

        /// <summary>
        /// Get user profile by ID
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetProfile([FromQuery] long id)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<UserProfileDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var user = await _userService.GetUserProfileAsync(id, CurrentUserId.Value);

                if (user == null)
                {
                    return Failure<UserProfileDto>(
                        null,
                        "User not found.",
                        StatusCodes.Status404NotFound);
                }

                return Success(user, "User profile retrieved successfully.");
            }
            catch (NotFoundException ex)
            {
                return Failure<UserProfileDto>(
                    null,
                    ex.Message,
                    StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting user profile - UserId: {UserId} | RequestedBy: {RequestedBy} | TraceId: {TraceId}",
                    id, CurrentUserId, TraceId);

                return Failure<UserProfileDto>(
                    null,
                    "An error occurred while getting user profile.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Update current user's profile
        /// </summary>
        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile(
            [FromBody] UpdateProfileDto request)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<UserProfileDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (!ModelState.IsValid)
                {
                    return Failure<UserProfileDto>(
                        null,
                        "Invalid profile data.",
                        StatusCodes.Status400BadRequest);
                }

                var updated = await _userService.UpdateProfileAsync(CurrentUserId.Value, request);

                if (updated != null)
                {
                    _logger.LogInformation(
                        "User profile updated - UserId: {UserId} | TraceId: {TraceId}",
                        CurrentUserId, TraceId);

                    return Success(updated, "Profile updated successfully.");
                }

                return Failure<UserProfileDto>(
                    null,
                    "Failed to update profile.",
                    StatusCodes.Status400BadRequest);
            }
            catch (ValidationException ex)
            {
                return Failure<UserProfileDto>(
                    null,
                    ex.Message,
                    StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating user profile - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<UserProfileDto>(
                    null,
                    "An error occurred while updating profile.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Update user's profile picture
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<string>>> UpdateProfilePicture(
         IFormFile file)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<string>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (file == null || file.Length == 0)
                {
                    return Failure<string>(
                        null,
                        "No file provided.",
                        StatusCodes.Status400BadRequest);
                }

                // Validate file size (e.g., max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return Failure<string>(
                        null,
                        "File size exceeds 5MB limit.",
                        StatusCodes.Status400BadRequest);
                }

                // Validate file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    return Failure<string>(
                        null,
                        "Invalid file type. Only JPEG, PNG, and WebP are allowed.",
                        StatusCodes.Status400BadRequest);
                }

                var imageUrl = await _userService.UpdateProfilePictureAsync(CurrentUserId.Value, file);

                _logger.LogInformation(
                    "Profile picture updated - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Success(imageUrl, "Profile picture updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating profile picture - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<string>(
                    null,
                    "An error occurred while updating profile picture.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Delete user's profile picture
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProfilePicture()
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<bool>(
                        false,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var deleted = await _userService.DeleteProfilePictureAsync(CurrentUserId.Value);

                if (deleted)
                {
                    _logger.LogInformation(
                        "Profile picture deleted - UserId: {UserId} | TraceId: {TraceId}",
                        CurrentUserId, TraceId);

                    return Success(true, "Profile picture deleted successfully.");
                }

                return Success(false, "No profile picture to delete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error deleting profile picture - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<bool>(
                    false,
                    "An error occurred while deleting profile picture.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(
            [FromBody] ChangePasswordDto request)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<bool>(
                        false,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (!ModelState.IsValid)
                {
                    return Failure<bool>(
                        false,
                        "Invalid password data.",
                        StatusCodes.Status400BadRequest);
                }

                var changed = await _authService.ChangePasswordAsync(request);

                if (changed)
                {
                    _logger.LogInformation(
                        "Password changed successfully - UserId: {UserId} | TraceId: {TraceId}",
                        CurrentUserId, TraceId);

                    return Success(true, "Password changed successfully. Please login again.");
                }

                return Failure<bool>(
                    false,
                    "Failed to change password. Please check your current password.",
                    StatusCodes.Status400BadRequest);
            }
            catch (ValidationException ex)
            {
                return Failure<bool>(
                    false,
                    ex.Message,
                    StatusCodes.Status400BadRequest);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<bool>(
                    false,
                    ex.Message,
                    StatusCodes.Status401Unauthorized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error changing password - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<bool>(
                    false,
                    "An error occurred while changing password.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Enable two-factor authentication
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<TwoFactorSetupDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<TwoFactorSetupDto>>> EnableTwoFactor()
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<TwoFactorSetupDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var setup = await _authService.EnableTwoFactorAsync(CurrentUserId.Value);

                _logger.LogInformation(
                    "Two-factor authentication setup initiated - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Success(setup, "Two-factor authentication setup initiated.");
            }
            catch (InvalidOperationException ex)
            {
                return Failure<TwoFactorSetupDto>(
                    null,
                    ex.Message,
                    StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error setting up two-factor authentication - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<TwoFactorSetupDto>(
                    null,
                    "An error occurred while setting up two-factor authentication.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Verify and complete two-factor authentication setup
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ConfirmTwoFactorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<ConfirmTwoFactorDto>>> VerifyTwoFactor(
            [FromBody] ConfirmTwoFactorDto request)
        {
            try
            {
                

                if (!ModelState.IsValid)
                {
                    return Failure<ConfirmTwoFactorDto>(
                        null,
                        "Invalid verification code.",
                        StatusCodes.Status400BadRequest);
                }

                var backupCodes = await _authService.ConfirmTwoFactorAsync(request);

                _logger.LogInformation(
                    "Two-factor authentication enabled - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Success(request, "Two-factor authentication enabled successfully.");
            }
            catch (ValidationException ex)
            {
                return Failure<ConfirmTwoFactorDto>(
                    null,
                    ex.Message,
                    StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error verifying two-factor authentication - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<ConfirmTwoFactorDto>(
                    null,
                    "An error occurred while verifying two-factor authentication.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Disable two-factor authentication
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<bool>>> DisableTwoFactor(
            [FromBody] string password)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<bool>(
                        false,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (!ModelState.IsValid)
                {
                    return Failure<bool>(
                        false,
                        "Invalid request data.",
                        StatusCodes.Status400BadRequest);
                }

                var disabled = await _authService.DisableTwoFactorAsync(
                    CurrentUserId.Value,
                    password);

                if (disabled)
                {
                    _logger.LogInformation(
                        "Two-factor authentication disabled - UserId: {UserId} | TraceId: {TraceId}",
                        CurrentUserId, TraceId);

                    return Success(true, "Two-factor authentication disabled successfully.");
                }

                return Failure<bool>(
                    false,
                    "Failed to disable two-factor authentication.",
                    StatusCodes.Status400BadRequest);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<bool>(
                    false,
                    ex.Message,
                    StatusCodes.Status401Unauthorized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error disabling two-factor authentication - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<bool>(
                    false,
                    "An error occurred while disabling two-factor authentication.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Delete user account
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteAccount(
            [FromBody] DeleteAccountDto request)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<bool>(
                        false,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (!ModelState.IsValid)
                {
                    return Failure<bool>(
                        false,
                        "Invalid request data.",
                        StatusCodes.Status400BadRequest);
                }

                var deleted = await _userService.DeleteAccountAsync(
                    CurrentUserId.Value,
                    request.Password,
                    request.Reason);

                if (deleted)
                {
                    _logger.LogInformation(
                        "User account deleted - UserId: {UserId} | Reason: {Reason} | TraceId: {TraceId}",
                        CurrentUserId, request.Reason, TraceId);

                    // Clear authentication
                    DeleteRefreshTokenCookie();

                    return Success(true, "Account deleted successfully.");
                }

                return Failure<bool>(
                    false,
                    "Failed to delete account. Please check your password.",
                    StatusCodes.Status400BadRequest);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<bool>(
                    false,
                    ex.Message,
                    StatusCodes.Status401Unauthorized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error deleting account - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<bool>(
                    false,
                    "An error occurred while deleting account.",
                    StatusCodes.Status500InternalServerError);
            }
        }
        

        /// <summary>
        /// Get user's security settings
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<UserSecuritySettingsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<UserSecuritySettingsDto>>> GetSecuritySettings()
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<UserSecuritySettingsDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var settings = await _userService.GetSecuritySettingsAsync(CurrentUserId.Value);

                return Success(settings, "Security settings retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting security settings - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<UserSecuritySettingsDto>(
                    null,
                    "An error occurred while getting security settings.",
                    StatusCodes.Status500InternalServerError);
            }
        }

    }
}