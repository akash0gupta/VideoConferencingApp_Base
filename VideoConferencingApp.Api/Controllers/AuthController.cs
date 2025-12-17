using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VideoConferencingApp.Api.DTOs;
using VideoConferencingApp.Api.DTOs.AuthDto;
using VideoConferencingApp.Api.DTOs.ContactDto;
using VideoConferencingApp.API.Controllers.Base;
using VideoConferencingApp.Application.Common.IAuthServices;
using VideoConferencingApp.Application.DTOs.Authentication;
using VideoConferencingApp.Application.DTOs.Notification;
using VideoConferencingApp.Application.DTOs.UserDto;
using VideoConferencingApp.Application.Services.UserServices;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Exceptions;
using VideoConferencingApp.Infrastructure.Services.AuthServices;

namespace VideoConferencingApp.Controllers
{
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly IJwtAuthenticationService _authenticationService;
        private readonly IUserDeviceTokenService _deviceTokenService;
        private readonly IMapper _mapper;

        public AuthController(
            IMapper mapper,
            IAuthService authService,
            IUserService userService,
            IJwtAuthenticationService authenticationService,
            IUserDeviceTokenService deviceTokenService,
            ICurrentUserService currentUserService,
            IHttpContextService httpContextService,
            IResponseHeaderService responseHeaderService,
            ILogger<AuthController> logger)
            : base(logger, currentUserService, httpContextService, responseHeaderService)
        {
            _mapper = mapper;
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _userService=userService ?? throw new ArgumentNullException(nameof(userService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _deviceTokenService = deviceTokenService ?? throw new ArgumentNullException(nameof(deviceTokenService));
        }


        /// <summary>
        /// Check if username is available
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<bool>>> CheckUsernameAvailability(
            [FromQuery] string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Success(false, "Username is required.");
                }

                var available = await _userService.IsUsernameAvailableAsync(username);

                return Success(
                    available,
                    available ? "Username is available." : "Username is already taken.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error checking username availability - Username: {Username} | TraceId: {TraceId}",
                    username, TraceId);

                return Success(false, "Unable to check username availability.");
            }
        }

        /// <summary>
        /// Check if email is available
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<bool>>> CheckEmailAvailability(
            [FromQuery] string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Success(false, "Email is required.");
                }

                var available = await _userService.IsEmailAvailableAsync(email);

                return Success(
                    available,
                    available ? "Email is available." : "Email is already registered.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error checking email availability - Email: {Email} | TraceId: {TraceId}",
                    email, TraceId);

                return Success(false, "Unable to check email availability.");
            }
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthenticationResultDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<AuthenticationResultDto>>> Register(
            [FromBody] RegisterRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Failure<AuthenticationResultDto>(null, "Invalid registration data provided.",
                        StatusCodes.Status400BadRequest);

                var register =new RegisterDto();
                register.DisplayName= string.IsNullOrWhiteSpace(request.FirstName) && string.IsNullOrWhiteSpace(request.LastName)
                    ? request.Username
                    : $"{request.FirstName} {request.LastName}".Trim();
                register.Username= request.Username;
                register.Email= request.Email;
                register.Password= request.Password;
                register.ConfirmPassword= request.ConfirmPassword;
                register.AcceptTerms= request.AcceptTerms;

                // Use services instead of direct HttpContext access
                register.IpAddress = ClientV4IpAddress;
                register.UserAgent = UserAgent;

                var result = await _authService.RegisterAsync(register);

                if (result.Success)
                {
                    if (request.DeviceInfo != null && !string.IsNullOrEmpty(request.DeviceInfo.DeviceToken))
                    {
                        try
                        {
                            await RegisterUserDeviceAsync(result.UserId, request.DeviceInfo);
                            result.DeviceRegistered = true;

                            _logger.LogInformation(
                                "Device registered during registration - UserId: {UserId} | TraceId: {TraceId}",
                                result.UserId, TraceId);
                        }
                        catch (Exception deviceEx)
                        {
                            _logger.LogWarning(deviceEx,
                                "Failed to register device during registration - UserId: {UserId} | TraceId: {TraceId}",
                                result.UserId, TraceId);
                        }
                    }

                    return StatusCode(StatusCodes.Status201Created, new ApiResponse<AuthenticationResultDto>
                    {
                        Success = true,
                        Message = "User registered successfully.",
                        Data = result,
                        TraceId = TraceId,
                        Timestamp = DateTime.UtcNow
                    });
                }

                return Failure<AuthenticationResultDto>(null, result.Message ?? "Registration failed.",
                    StatusCodes.Status400BadRequest);
            }
            catch (ValidationException ex)
            {
                return Failure<AuthenticationResultDto>(null, ex.Message, StatusCodes.Status400BadRequest);
            }
            catch (TooManyRequestsException ex)
            {
                // Use ResponseHeaderService to set Retry-After header
                _responseHeaderService.SetRetryAfter(ex.RetryAfterSeconds);

                return Failure<AuthenticationResultDto>(null, ex.Message, StatusCodes.Status429TooManyRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration - TraceId: {TraceId}", TraceId);
                return Failure<AuthenticationResultDto>(null,
                    "An internal error occurred during registration.",
                    StatusCodes.Status500InternalServerError);
            }
        }



        /// <summary>
        /// Authenticate user and get access token
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthenticationResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<AuthenticationResultDto>>> Login(
            [FromBody] LoginRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Failure<AuthenticationResultDto>(null, "Invalid login data provided.",
                        StatusCodes.Status400BadRequest);

                var login = new LoginDto();
                login.RememberMe= request.RememberMe;
                login.Password= request.Password;
                login.UsernameOrEmail= request.UsernameOrEmail;
                login.IpAddress = ClientV4IpAddress;
                login.UserAgent = UserAgent;

                var result = await _authService.LoginAsync(login);

                if (result.Success)
                {
                    if (request.DeviceInfo != null && !string.IsNullOrEmpty(request.DeviceInfo.DeviceToken))
                    {
                        try
                        {
                            await RegisterUserDeviceAsync(result.UserId, request.DeviceInfo);
                            result.DeviceRegistered = true;
                            await _deviceTokenService.UpdateLastUsedAsync(request.DeviceInfo.DeviceToken);
                        }
                        catch (Exception deviceEx)
                        {
                            _logger.LogWarning(deviceEx,
                                "Failed to register device - UserId: {UserId} | TraceId: {TraceId}",
                                result.UserId, TraceId);
                        }
                    }
                    
                    // Use helper method for cookie
                    SetRefreshTokenCookie(result.RefreshToken);

                    return Success(result, "Login successful.");
                }

                if (result.RequiresTwoFactor || result.RequiresPasswordChange)
                {
                    return Success(result, "Additional authentication required.");
                }

                return Failure<AuthenticationResultDto>(null, result.Message ?? "Invalid credentials.",
                    StatusCodes.Status401Unauthorized);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<AuthenticationResultDto>(null, ex.Message, StatusCodes.Status401Unauthorized);
            }
            catch (TooManyRequestsException ex)
            {
                _responseHeaderService.SetRetryAfter(ex.RetryAfterSeconds);
                return Failure<AuthenticationResultDto>(null, ex.Message, StatusCodes.Status429TooManyRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login - TraceId: {TraceId}", TraceId);
                return Failure<AuthenticationResultDto>(null,
                    "An internal error occurred during login.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Verify two-factor authentication code
        /// </summary>
        /// <param name="request">2FA verification request</param>
        /// <returns>Authentication result with tokens</returns>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthenticationResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<AuthenticationResultDto>>> VerifyTwoFactor([FromBody] VerifyTwoFactorDto request)
        {
            try
            {
                

                return Ok(new { message = "2FA verification endpoint" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during 2FA verification");
  
                return Failure<AuthenticationResultDto>(null, "An error occurred during 2FA verification",
                   StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Refresh access token
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthenticationResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<AuthenticationResultDto>>> RefreshToken(
            [FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                // Use helper method to get cookie
                request.RefreshToken ??= GetRefreshTokenFromCookie();

                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return Failure<AuthenticationResultDto>(null, "Refresh token is required.",
                        StatusCodes.Status401Unauthorized);
                }

                request.IpAddress = ClientV4IpAddress;
                request.UserAgent = UserAgent;

                var result = await _authService.RefreshTokenAsync(request);

                if (result.Success)
                {
                    if (!string.IsNullOrEmpty(request.DeviceToken))
                    {
                        try
                        {
                            await _deviceTokenService.UpdateLastUsedAsync(request.DeviceToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "Failed to update device last used - TraceId: {TraceId}", TraceId);
                        }
                    }

                    if (!string.IsNullOrEmpty(result.RefreshToken))
                    {
                        SetRefreshTokenCookie(result.RefreshToken);
                    }

                    return Success(result, "Token refreshed successfully.");
                }

                return Failure<AuthenticationResultDto>(null, "Invalid refresh token.",
                    StatusCodes.Status401Unauthorized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ex.Message} Error refreshing token - TraceId: {TraceId}", TraceId);
                return Failure<AuthenticationResultDto>(null,
                    ex.Message,
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutDto request = null)
        {
            try
            {
                request ??= new LogoutDto();
                request.RefreshToken ??= GetRefreshTokenFromCookie();

                if (!string.IsNullOrEmpty(request.DeviceToken))
                {
                    try
                    {
                        var deactivated = await _deviceTokenService.DeactivateDeviceAsync(request.DeviceToken);

                        if (deactivated)
                        {
                            _logger.LogInformation(
                                "Device deactivated - TraceId: {TraceId}", TraceId);
                        }
                    }
                    catch (Exception deviceEx)
                    {
                        _logger.LogWarning(deviceEx,
                            "Failed to deactivate device - TraceId: {TraceId}", TraceId);
                    }
                }

                await _authService.LogoutAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Non-critical error during logout - TraceId: {TraceId}", TraceId);
            }
            finally
            {
                DeleteRefreshTokenCookie();
            }

            return Success<object>(null, "Logged out successfully.");
        }

        /// <summary>
        /// Get current authenticated user
        /// </summary>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            try
            {
                // Use ICurrentUserService instead of manual check
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<UserDto>(null, "User not authenticated.",
                        StatusCodes.Status401Unauthorized);
                }

                var user = await _authenticationService.GetAuthenticatedUserAsync();
                if (user == null)
                {
                    return Failure<UserDto>(null, "Authenticated user not found.",
                        StatusCodes.Status401Unauthorized);
                }

                //var devices = await _deviceTokenService.GetUserDevicesAsync(user.Id, activeOnly: true);

                var userDto = user.Adapt<UserDto>();

                return Success(userDto, "User retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user - TraceId: {TraceId}", TraceId);
                return Failure<UserDto>(null,
                    "An error occurred while getting user information.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get current authenticated user
        /// </summary>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<IList<UserDeviceToken>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IList<UserDeviceToken>>>> GetCurrentDevices()
        {
            try
            {
                // Use ICurrentUserService instead of manual check
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IList<UserDeviceToken>>(null, "User not authenticated.",
                        StatusCodes.Status401Unauthorized);
                }

                if (CurrentUserId == null || CurrentUserId==0)
                {
                    return Failure<IList<UserDeviceToken>>(null, "Authenticated user not found.",
                        StatusCodes.Status401Unauthorized);
                }

                var devices = await _deviceTokenService.GetUserDevicesAsync(CurrentUserId ?? 0, activeOnly: true);

                return Success(devices, "User retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user - TraceId: {TraceId}", TraceId);
                return Failure<IList<UserDeviceToken>>(null,
                    "An error occurred while getting user information.",
                    StatusCodes.Status500InternalServerError);
            }
        }

       
        /// <summary>
        /// Request password reset email
        /// </summary>
        /// <param name="request">Password reset request</param>
        /// <returns>Confirmation message</returns>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] PasswordResetRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                request.IpAddress = ClientV4IpAddress;

                await _authService.RequestPasswordResetAsync(request);

                return Success<object>(null, "If the email exists, a password reset link has been sent.");
            }
            catch (TooManyRequestsException ex)
            {
                return Failure<object>("RATE_LIMIT_EXCEEDED", ex.Message,
                      StatusCodes.Status429TooManyRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset request");
              
                return Failure<object>(null,"Error during password reset request",
                     StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Reset password using token
        /// </summary>
        /// <param name="request">Password reset details</param>
        /// <returns>Confirmation message</returns>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordDto request)
        {
            try
            {
               
                var result = await _authService.ResetPasswordAsync(request);

                if (result)
                {
                    return Success<object>(null,"Password has been reset successfully");
                }

                return Failure<object>(null, "Failed to reset password");
            }
            catch (ValidationException ex)
            {
                return Failure<object>(null,ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return Failure<object>("An error occurred while resetting password", ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Verify email address
        /// </summary>
        /// <param name="request">Email verification token</param>
        /// <returns>Confirmation message</returns>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<object>>> VerifyEmail([FromBody] VerifyEmailDto request)
        {
            try
            {
                var result = await _authService.VerifyEmailAsync(request);

                if (result)
                {
                    return Ok(new { message = "Email verified successfully" });
                }

                return Failure<object>(null,"Failed to verify email");

            }
            catch (ValidationException ex)
            {
                return Failure<object>(null, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email");
                return Failure<object>(null, ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Resend email verification
        /// </summary>
        /// <param name="request">Email to resend verification</param>
        /// <returns>Confirmation message</returns>
        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
        public async Task<ActionResult<ApiResponse<object>>> ResendVerification([FromBody] ResendVerificationDto request)
        {
            try
            {
                await _authService.ResendVerificationEmailAsync(request);
                return Success<object>(null, "If the email exists and is unverified, a verification link has been sent.");
            }
            catch (TooManyRequestsException ex)
            {
                Response.Headers.Add("Retry-After", ex.RetryAfterSeconds.ToString());

                return Failure<object>("RATE_LIMIT_EXCEEDED", ex.Message,
                      StatusCodes.Status429TooManyRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email");
                return Failure<object>(null, "If the email exists and is unverified, a verification link has been sent.",
                      StatusCodes.Status500InternalServerError);
            }
        }

       
       
        #region Helper Methods

        private async Task<UserDeviceToken> RegisterUserDeviceAsync(long userId, DeviceInfo deviceInfoDto)
        {
            var deviceInfo = new DeviceInfo
            {
                DeviceToken = deviceInfoDto.DeviceToken,
                Platform = deviceInfoDto.Platform,
                DeviceId = deviceInfoDto.DeviceId,
                DeviceName = deviceInfoDto.DeviceName,
                DeviceModel = deviceInfoDto.DeviceModel,
                OsVersion = deviceInfoDto.OsVersion,
                AppVersion = deviceInfoDto.AppVersion
            };

            return await _deviceTokenService.RegisterDeviceAsync(userId, deviceInfo);
        }

        #endregion
    }
}