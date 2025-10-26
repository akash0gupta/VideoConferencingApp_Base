using System.Threading.Tasks;
using VideoConferencingApp.Domain.DTOs.Authentication;
using VideoConferencingApp.Domain.Entities;

namespace VideoConferencingApp.Application.Interfaces.Common.IAuthServices
{
    public interface IAuthService
    {
        #region Registration

        /// <summary>
        /// Register a new user with comprehensive validation
        /// </summary>
        /// <param name="request">Registration request containing user details</param>
        /// <returns>Authentication result with tokens if successful</returns>
        Task<AuthenticationResultDto> RegisterAsync(RegisterRequestDto request);

        #endregion

        #region Login

        /// <summary>
        /// Authenticate user with comprehensive security checks
        /// </summary>
        /// <param name="request">Login request with credentials</param>
        /// <returns>Authentication result with tokens and user information</returns>
        Task<AuthenticationResultDto> LoginAsync(LoginRequestDto request);

        #endregion

        #region Logout

        /// <summary>
        /// Logout user and invalidate tokens
        /// </summary>
        /// <param name="request">Logout request with token and session information</param>
        /// <returns>True if logout successful</returns>
        Task<bool> LogoutAsync(LogoutRequestDto request);

        /// <summary>
        /// Logout from all devices by invalidating all tokens and sessions
        /// </summary>
        /// <param name="userId">User ID to logout</param>
        /// <returns>True if logout successful</returns>
        Task<bool> LogoutFromAllDevicesAsync(long userId);

        #endregion

        #region Token Management

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        /// <param name="request">Refresh token request</param>
        /// <returns>New authentication result with refreshed tokens</returns>
        Task<AuthenticationResultDto> RefreshTokenAsync(RefreshTokenRequestDto request);

        #endregion

        #region Password Management

        /// <summary>
        /// Request password reset by sending reset token to email
        /// </summary>
        /// <param name="request">Password reset request with email</param>
        /// <returns>True if request processed (always returns true for security)</returns>
        Task<bool> RequestPasswordResetAsync(PasswordResetRequestDto request);

        /// <summary>
        /// Reset password using token received via email
        /// </summary>
        /// <param name="request">Reset password request with token and new password</param>
        /// <returns>True if password reset successful</returns>
        Task<bool> ResetPasswordAsync(ResetPasswordDto request);

        /// <summary>
        /// Change password for authenticated user
        /// </summary>
        /// <param name="request">Change password request with current and new password</param>
        /// <returns>True if password changed successfully</returns>
        Task<bool> ChangePasswordAsync(ChangePasswordDto request);

        #endregion

        #region Email Verification

        /// <summary>
        /// Verify email address using verification token
        /// </summary>
        /// <param name="request">Email verification request with token</param>
        /// <returns>True if email verified successfully</returns>
        Task<bool> VerifyEmailAsync(VerifyEmailDto request);

        /// <summary>
        /// Resend verification email to user
        /// </summary>
        /// <param name="request">Resend verification request with email</param>
        /// <returns>True if email sent (always returns true for security)</returns>
        Task<bool> ResendVerificationEmailAsync(ResendVerificationDto request);

        #endregion

        #region Two-Factor Authentication

        /// <summary>
        /// Enable two-factor authentication for user
        /// </summary>
        /// <param name="userId">User ID to enable 2FA for</param>
        /// <returns>Two-factor setup information including QR code</returns>
        Task<TwoFactorSetupDto> EnableTwoFactorAsync(long userId);

        /// <summary>
        /// Confirm two-factor authentication setup with verification code
        /// </summary>
        /// <param name="request">Confirmation request with verification code</param>
        /// <returns>True if 2FA enabled successfully</returns>
        Task<bool> ConfirmTwoFactorAsync(ConfirmTwoFactorDto request);

        #endregion
    }
}
