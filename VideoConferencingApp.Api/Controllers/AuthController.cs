using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using VideoConferencingApp.Api.Models;
using VideoConferencingApp.Application.Interfaces.Common.IAuthServices;
using VideoConferencingApp.Domain.DTOs;
using VideoConferencingApp.Domain.DTOs.Authentication;
using VideoConferencingApp.Domain.Exceptions;

namespace VideoConferencingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtAuthenticationService _authenticationService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IJwtAuthenticationService authenticationService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _authenticationService = authenticationService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="request">Registration details</param>
        /// <returns>Authentication result with tokens</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthenticationResultDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Add client info
                request.IpAddress = GetIpAddress();
                request.UserAgent = Request.Headers["User-Agent"].ToString();

                var result = await _authService.RegisterAsync(request);

                if (result.Success)
                {
                    return CreatedAtAction(nameof(GetCurrentUser), new { id = result.UserId }, result);
                }

                return BadRequest(new ErrorResponse
                {
                    Message = result.Message,
                    Code = "REGISTRATION_FAILED"
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "VALIDATION_ERROR",
                    Details = ex.Errors
                });
            }
            catch (TooManyRequestsException ex)
            {
                Response.Headers.Add("Retry-After", ex.RetryAfterSeconds.ToString());
                return StatusCode(StatusCodes.Status429TooManyRequests, new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "RATE_LIMIT_EXCEEDED"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred during registration",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Authenticate user and get access token
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>Authentication result with tokens</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthenticationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                request.IpAddress = GetIpAddress();
                request.UserAgent = Request.Headers["User-Agent"].ToString();

                var result = await _authService.LoginAsync(request);

                if (result.Success)
                {
                    // Set refresh token in HTTP-only cookie
                    SetRefreshTokenCookie(result.RefreshToken);

                    return Ok(result);
                }

                if (result.RequiresTwoFactor)
                {
                    return Ok(new
                    {
                        requiresTwoFactor = true,
                        message = result.Message
                    });
                }

                if (result.RequiresPasswordChange)
                {
                    return Ok(new
                    {
                        requiresPasswordChange = true,
                        message = result.Message
                    });
                }

                return Unauthorized(new ErrorResponse
                {
                    Message = result.Message ?? "Invalid credentials",
                    Code = "LOGIN_FAILED"
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "UNAUTHORIZED"
                });
            }
            catch (TooManyRequestsException ex)
            {
                Response.Headers.Add("Retry-After", ex.RetryAfterSeconds.ToString());
                return StatusCode(StatusCodes.Status429TooManyRequests, new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "RATE_LIMIT_EXCEEDED"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred during login",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Verify two-factor authentication code
        /// </summary>
        /// <param name="request">2FA verification request</param>
        /// <returns>Authentication result with tokens</returns>
        [HttpPost("verify-2fa")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthenticationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorDto request)
        {
            try
            {
                // Implementation would verify the 2FA code and complete login
                // This is a placeholder for the actual implementation

                return Ok(new { message = "2FA verification endpoint" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during 2FA verification");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred during 2FA verification",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <returns>New authentication tokens</returns>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthenticationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            try
            {
                // Try to get refresh token from cookie if not provided
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    request.RefreshToken = Request.Cookies["refreshToken"];
                }

                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    return Unauthorized(new ErrorResponse
                    {
                        Message = "Refresh token is required",
                        Code = "MISSING_TOKEN"
                    });
                }

                request.IpAddress = GetIpAddress();
                request.UserAgent = Request.Headers["User-Agent"].ToString();

                var result = await _authService.RefreshTokenAsync(request);

                if (result.Success)
                {
                    // Update refresh token cookie if rotated
                    if (!string.IsNullOrEmpty(result.RefreshToken))
                    {
                        SetRefreshTokenCookie(result.RefreshToken);
                    }

                    return Ok(result);
                }

                return Unauthorized(new ErrorResponse
                {
                    Message = "Invalid refresh token",
                    Code = "INVALID_TOKEN"
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "UNAUTHORIZED"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while refreshing token",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        /// <returns>Logout confirmation</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request = null)
        {
            try
            {
                request ??= new LogoutRequestDto();

                // Get refresh token from cookie if not provided
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    request.RefreshToken = Request.Cookies["refreshToken"];
                }

                var result = await _authService.LogoutAsync(request);

                // Clear refresh token cookie
                Response.Cookies.Delete("refreshToken");

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return Ok(new { message = "Logged out" }); // Don't expose errors during logout
            }
        }

        /// <summary>
        /// Logout from all devices
        /// </summary>
        /// <returns>Logout confirmation</returns>
        [HttpPost("logout-all")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LogoutFromAllDevices()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                await _authService.LogoutFromAllDevicesAsync(userId.Value);

                // Clear refresh token cookie
                Response.Cookies.Delete("refreshToken");

                return Ok(new { message = "Logged out from all devices successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout from all devices");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred during logout",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Request password reset email
        /// </summary>
        /// <param name="request">Password reset request</param>
        /// <returns>Confirmation message</returns>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ForgotPassword([FromBody] PasswordResetRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                request.IpAddress = GetIpAddress();

                await _authService.RequestPasswordResetAsync(request);

                // Always return success to prevent email enumeration
                return Ok(new
                {
                    message = "If the email exists, a password reset link has been sent."
                });
            }
            catch (TooManyRequestsException ex)
            {
                Response.Headers.Add("Retry-After", ex.RetryAfterSeconds.ToString());
                return StatusCode(StatusCodes.Status429TooManyRequests, new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "RATE_LIMIT_EXCEEDED"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset request");
                // Don't expose errors to prevent email enumeration
                return Ok(new
                {
                    message = "If the email exists, a password reset link has been sent."
                });
            }
        }

        /// <summary>
        /// Reset password using token
        /// </summary>
        /// <param name="request">Password reset details</param>
        /// <returns>Confirmation message</returns>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _authService.ResetPasswordAsync(request);

                if (result)
                {
                    return Ok(new { message = "Password has been reset successfully" });
                }

                return BadRequest(new ErrorResponse
                {
                    Message = "Failed to reset password",
                    Code = "RESET_FAILED"
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "VALIDATION_ERROR"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while resetting password",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Change password for authenticated user
        /// </summary>
        /// <param name="request">Password change details</param>
        /// <returns>Confirmation message</returns>
        [HttpPost("change-password")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _authService.ChangePasswordAsync(request);

                if (result)
                {
                    return Ok(new { message = "Password changed successfully" });
                }

                return BadRequest(new ErrorResponse
                {
                    Message = "Failed to change password",
                    Code = "CHANGE_FAILED"
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "VALIDATION_ERROR"
                });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "UNAUTHORIZED"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while changing password",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Verify email address
        /// </summary>
        /// <param name="request">Email verification token</param>
        /// <returns>Confirmation message</returns>
        [HttpPost("verify-email")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _authService.VerifyEmailAsync(request);

                if (result)
                {
                    return Ok(new { message = "Email verified successfully" });
                }

                return BadRequest(new ErrorResponse
                {
                    Message = "Failed to verify email",
                    Code = "VERIFICATION_FAILED"
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "VALIDATION_ERROR"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while verifying email",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Resend email verification
        /// </summary>
        /// <param name="request">Email to resend verification</param>
        /// <returns>Confirmation message</returns>
        [HttpPost("resend-verification")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                await _authService.ResendVerificationEmailAsync(request);

                // Always return success to prevent email enumeration
                return Ok(new
                {
                    message = "If the email exists and is unverified, a verification link has been sent."
                });
            }
            catch (TooManyRequestsException ex)
            {
                Response.Headers.Add("Retry-After", ex.RetryAfterSeconds.ToString());
                return StatusCode(StatusCodes.Status429TooManyRequests, new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "RATE_LIMIT_EXCEEDED"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification email");
                // Don't expose errors to prevent email enumeration
                return Ok(new
                {
                    message = "If the email exists and is unverified, a verification link has been sent."
                });
            }
        }

        /// <summary>
        /// Enable two-factor authentication
        /// </summary>
        /// <returns>2FA setup information</returns>
        [HttpPost("enable-2fa")]
        [Authorize]
        [ProducesResponseType(typeof(TwoFactorSetupDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> EnableTwoFactor()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var result = await _authService.EnableTwoFactorAsync(userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling 2FA");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while enabling 2FA",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Confirm two-factor authentication setup
        /// </summary>
        /// <param name="request">2FA confirmation details</param>
        /// <returns>Confirmation message</returns>
        [HttpPost("confirm-2fa")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfirmTwoFactor([FromBody] ConfirmTwoFactorDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                request.UserId = userId.Value;
                var result = await _authService.ConfirmTwoFactorAsync(request);

                if (result)
                {
                    return Ok(new { message = "Two-factor authentication enabled successfully" });
                }

                return BadRequest(new ErrorResponse
                {
                    Message = "Failed to enable 2FA",
                    Code = "2FA_SETUP_FAILED"
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "VALIDATION_ERROR"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming 2FA");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while confirming 2FA",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get current authenticated user
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var user = await _authenticationService.GetAuthenticatedUserAsync();
                if (user == null)
                {
                    return Unauthorized();
                }

                return Ok(new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Bio = user.Bio,
                    Role = user.Role.ToString(),
                    EmailVerified = user.EmailVerified,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    CreatedAt = user.CreatedOnUtc
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while getting user information",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        #region Helper Methods

        private void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Set to true in production with HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }

        private string GetIpAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                return Request.Headers["X-Forwarded-For"].FirstOrDefault();
            }
            else
            {
                return HttpContext.Connection.RemoteIpAddress?.ToString();
            }
        }

        private long? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }

        #endregion
    }
}