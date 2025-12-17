using BCrypt.Net;
using LinqToDB;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OtpNet;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using VideoConferencingApp.Application.DTOs.Authentication;
using VideoConferencingApp.Application.DTOs.Notification;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Events;
using VideoConferencingApp.Domain.Events.Notification;
using VideoConferencingApp.Domain.Events.UserEvent;
using VideoConferencingApp.Domain.Exceptions;
using VideoConferencingApp.Domain.Interfaces;
using VideoConferencingApp.Infrastructure.Configuration;
using VideoConferencingApp.Infrastructure.Configuration.Settings;
using VideoConferencingApp.Infrastructure.Extensions;
using VideoConferencingApp.Application.Events;
using VideoConferencingApp.Domain.CacheKeys;
using VideoConferencingApp.Application.Common.IAuthServices;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Application.Common.IEventHandlerServices;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Application.Services.UserServices;

namespace VideoConferencingApp.Infrastructure.Services.AuthServices
{


    public class AuthService : IAuthService
    {
        #region Fields

        private readonly IUserService _userService;
        private readonly IBCryptPasswordServices _bryptPasswordServices;
        private readonly IRepository<User> _userRepository;
        private readonly IJwtAuthenticationService _authenticationService;
        private readonly ILogger<AuthService> _logger;
        private readonly JwtSettings _jwtSettings;
        private readonly IEventPublisher _eventPublisher; // Changed from IEmailService
        private readonly IStaticCacheManager _cache; // Changed from IMemoryCache
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<RefreshToken> _refreshTokenRepository;
        private readonly IRepository<LoginAttempt> _loginAttemptRepository;
        private readonly IRepository<UserSession> _sessionRepository;
        private readonly SecuritySettings _securitySettings;



        #endregion

        #region Constructor

        public AuthService(
            IUserService userService,
            AppSettings appSettings,
            IJwtAuthenticationService authenticationService,
            IBCryptPasswordServices bryptPasswordServices,
            ILogger<AuthService> logger,
            IEventPublisher eventPublisher,
            IStaticCacheManager cache,
            IUnitOfWork unitOfWork,
            IRepository<User> userRepository,
            IRepository<RefreshToken> refreshTokenRepository,
            IRepository<LoginAttempt> loginAttemptRepository,
            IRepository<UserSession> sessionRepository)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
            _loginAttemptRepository = loginAttemptRepository ?? throw new ArgumentNullException(nameof(loginAttemptRepository));
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            _bryptPasswordServices = bryptPasswordServices ?? throw new ArgumentNullException(nameof(bryptPasswordServices));
            _jwtSettings = appSettings.Get<JwtSettings>();
            _securitySettings = appSettings.Get<SecuritySettings>();
        }

        #endregion

        #region Registration

        /// <summary>
        /// Register a new user with comprehensive validation
        /// </summary>
        public async Task<AuthenticationResultDto> RegisterAsync(RegisterDto request)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Validate input
                await ValidateRegistrationAsync(request);

                // Check rate limiting
                await CheckRateLimitAsync($"register_{request.Email}", 3, TimeSpan.FromHours(1));

                // Hash the password with strong parameters
                var passwordHash = _bryptPasswordServices.HashPassword(request.Password);

                // Generate verification token
                var emailVerificationToken = GenerateSecureToken();
                var emailVerificationExpiry = DateTime.UtcNow.AddDays(AuthCacheKey.EMAIL_VERIFICATION_TOKEN_EXPIRY_DAYS);

                // Create user entity
                var newUser = new User
                {
                    Username = request.Username.ToLower().Trim(),
                    Email = request.Email.ToLower().Trim(),
                    DisplayName = request.DisplayName ?? request.Username,
                    PasswordHash = passwordHash,
                    Role = UserRole.Participant,
                    IsActive = !_securitySettings.RequireEmailVerification,
                    EmailVerified = false,
                    EmailVerificationToken = emailVerificationToken,
                    EmailVerificationTokenExpiry = emailVerificationExpiry,
                    CreatedOnUtc = DateTime.UtcNow,
                    LastPasswordChangeAt = DateTime.UtcNow,
                    TwoFactorEnabled = false,
                    LockoutEnabled = true,
                    AccessFailedCount = 0,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    RegistrationIp = request.IpAddress,
                    RegistrationUserAgent = request.UserAgent
                };

                // Create user
                var createdUser = await _userService.CreateUserAsync(newUser);

                if (createdUser == null || createdUser.Id == 0)
                {
                    throw new InvalidOperationException("User registration failed");
                }

                // Send verification email via event
                await PublishEmailVerificationEventAsync(createdUser, emailVerificationToken);

                // Publish registration event
                await _eventPublisher.PublishAsync(new UserRegisteredEvent
                {
                    UserId = createdUser.Id,
                    Username = createdUser.Username,
                    Email = createdUser.Email,
                    Timestamp = DateTime.UtcNow,
                    EventId = Guid.NewGuid()
                });

                // Generate tokens if email verification is not required
                string accessToken = null;
                string refreshToken = null;

                if (!_securitySettings.RequireEmailVerification)
                {
                    accessToken = await _authenticationService.GenerateTokenAsync(createdUser);
                    refreshToken = await GenerateAndSaveRefreshTokenAsync(createdUser, request.IpAddress, request.UserAgent);
                }

                await transaction.CommitAsync();

                _logger.LogInformation("New user registered: {Username} ({Email})", createdUser.Username, createdUser.Email);

                return new AuthenticationResultDto
                {
                    Success = true,
                    UserId = createdUser.Id,
                    Username = createdUser.Username,
                    Email = createdUser.Email,
                    DisplayName = createdUser.DisplayName,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = accessToken != null ? _jwtSettings.ExpiryInMinutes * 60 : 0,
                    RequiresEmailVerification = _securitySettings.RequireEmailVerification,
                    Message = _securitySettings.RequireEmailVerification
                        ? "Registration successful. Please check your email to verify your account."
                        : "Registration successful."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during user registration for {Email}", request.Email);
                throw;
            }
        }

        #endregion

        #region Login

        /// <summary>
        /// Authenticate user with comprehensive security checks
        /// </summary>
        public async Task<AuthenticationResultDto> LoginAsync(LoginDto request)
        {

            // Validate input
            if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ValidationException("Username/email and password are required");
            }

            // Check rate limiting
            await CheckRateLimitAsync($"login_{request.IpAddress}", 10, TimeSpan.FromMinutes(15));

            // Find user
            var user = await FindUserByUsernameOrEmailAsync(request.UsernameOrEmail);

            if (user == null)
            {
                await RecordFailedLoginAttemptAsync(request.UsernameOrEmail, request.IpAddress, "User not found");
                throw new UnauthorizedException("Invalid credentials");
            }

            // Check if account is locked
            if (await IsAccountLockedAsync(user))
            {
                throw new UnauthorizedException("Account is locked. Please try again later or contact support.");
            }

            // Check if email verification is required
            if (_securitySettings.RequireEmailVerification && !user.EmailVerified)
            {
                throw new UnauthorizedException("Email verification required. Please check your email.");
            }

            // Check if account is active
            if (!user.IsActive)
            {
                throw new UnauthorizedException("Account is deactivated. Please contact support.");
            }

            // Verify password
            if (!_bryptPasswordServices.VerifyPassword(request.Password, user.PasswordHash))
            {
                await RecordFailedLoginAttemptAsync(user.Username, request.IpAddress, "Invalid password");
                await IncrementAccessFailedCountAsync(user);
                throw new UnauthorizedException("Invalid credentials");
            }

            // Check if 2FA is enabled
            if (user.TwoFactorEnabled && string.IsNullOrEmpty(request.TwoFactorCode))
            {
                // Generate and send 2FA code
                var twoFactorCode = await GenerateTwoFactorCodeAsync(user);
                await SendTwoFactorCodeAsync(user, twoFactorCode);

                return new AuthenticationResultDto
                {
                    Success = false,
                    RequiresTwoFactor = true,
                    Message = "Two-factor authentication code sent to your registered device."
                };
            }

            // Verify 2FA code if provided
            if (user.TwoFactorEnabled && !string.IsNullOrEmpty(request.TwoFactorCode))
            {
                if (!await VerifyTwoFactorCodeAsync(user, request.TwoFactorCode))
                {
                    await RecordFailedLoginAttemptAsync(user.Username, request.IpAddress, "Invalid 2FA code");
                    throw new UnauthorizedException("Invalid two-factor authentication code");
                }
            }

            // Check for password expiry
            if (_securitySettings.PasswordExpiryDays > 0)
            {
                var daysSincePasswordChange = (DateTime.UtcNow - user.LastPasswordChangeAt).TotalDays;
                if (daysSincePasswordChange > _securitySettings.PasswordExpiryDays)
                {
                    return new AuthenticationResultDto
                    {
                        Success = false,
                        RequiresPasswordChange = true,
                        Message = "Your password has expired. Please change your password."
                    };
                }
            }

            // Reset failed attempt count
            await ResetAccessFailedCountAsync(user);

            // Generate tokens
            var accessToken = await _authenticationService.GenerateTokenAsync(user);
            var refreshToken = await GenerateAndSaveRefreshTokenAsync(user, request.IpAddress, request.UserAgent);

            // Create session
            var session = await CreateUserSessionAsync(user, request.IpAddress, request.UserAgent, refreshToken);

            // Update last login
            await UpdateLastLoginAsync(user, request.IpAddress);

            // Record successful login
            await RecordSuccessfulLoginAsync(user, request.IpAddress);

            // Publish login event
            await _eventPublisher.PublishAsync(new UserLoggedInEvent
            {
                UserId = user.Id,
                Username = user.Username,
                SessionId = session.SessionId,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                Timestamp = DateTime.UtcNow,
                EventId = Guid.NewGuid()
            });

            // Send login notification email
            await PublishLoginNotificationEventAsync(user, request.IpAddress, GetDeviceName(request.UserAgent));

            _logger.LogInformation("User {UserId} successfully logged in from {IpAddress}", user.Id, request.IpAddress);

            return new AuthenticationResultDto
            {
                Success = true,
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _jwtSettings.ExpiryInMinutes * 60,
                SessionId = session.SessionId,
                Role = nameof(user.Role),
                Permissions = await GetUserPermissionsAsync(user)
            };

        }

        #endregion

        #region Logout

        /// <summary>
        /// Logout user and invalidate tokens
        /// </summary>
        public async Task<bool> LogoutAsync(LogoutDto request)
        {
            try
            {
                var user = await _authenticationService.GetAuthenticatedUserAsync();
                if (user == null)
                {
                    return false;
                }

                // Revoke refresh token
                if (!string.IsNullOrEmpty(request.RefreshToken))
                {
                    await RevokeRefreshTokenAsync(request.RefreshToken);
                }

                // End session
                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    await EndUserSessionAsync(request.SessionId);
                }

                // Sign out from authentication service
                await _authenticationService.SignOutAsync();

                // Publish logout event
                await _eventPublisher.PublishAsync(new UserLoggedOutEvent
                {
                    UserId = user.Id,
                    Username = user.Username,
                    SessionId = request.SessionId,
                    Timestamp = DateTime.UtcNow,
                    EventId = Guid.NewGuid()
                });

                _logger.LogInformation("User {UserId} logged out", user.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                throw;
            }
        }

        /// <summary>
        /// Logout from all devices
        /// </summary>
        public async Task<bool> LogoutFromAllDevicesAsync(long userId)
        {
            try
            {
                // Revoke all refresh tokens
                var refreshTokens = await _refreshTokenRepository.FindAsync(rt =>
                    rt.UserId == userId && !rt.IsRevoked);

                foreach (var token in refreshTokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                    await _refreshTokenRepository.UpdateAsync(token);
                }

                // End all sessions
                var sessions = await _sessionRepository.FindAsync(s =>
                    s.UserId == userId && s.IsActive);

                foreach (var session in sessions)
                {
                    session.IsActive = false;
                    session.EndedAt = DateTime.UtcNow;
                    await _sessionRepository.UpdateAsync(session);
                }

                // Update user security stamp to invalidate all existing tokens
                var user = await _userService.GetByIdAsync(userId);
                if (user != null)
                {
                    user.SecurityStamp = Guid.NewGuid().ToString();
                    await _userService.UpdateUserAsync(user);
                }

                _logger.LogInformation("User {UserId} logged out from all devices", userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout from all devices for user {UserId}", userId);
                throw;
            }
        }

        #endregion

        #region Token Management

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        public async Task<AuthenticationResultDto> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            try
            {
                // Validate refresh token
                var storedToken = await _refreshTokenRepository.Table
                    .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

                if (storedToken == null || storedToken.IsRevoked)
                {
                    throw new UnauthorizedException("Invalid refresh token");
                }

                if (storedToken.ExpiresAt < DateTime.UtcNow)
                {
                    throw new UnauthorizedException("Refresh token has expired");
                }

                // Get user
                var user = await _userService.GetByIdAsync(storedToken.UserId);
                if (user == null || !user.IsActive)
                {
                    throw new UnauthorizedException("User not found or inactive");
                }

                // Rotate refresh token (optional security measure)
                if (_securitySettings.RotateRefreshTokens)
                {
                    storedToken.IsRevoked = true;
                    storedToken.RevokedAt = DateTime.UtcNow;
                    storedToken.ReplacedByToken = GenerateSecureToken();
                    await _refreshTokenRepository.UpdateAsync(storedToken);

                    // Create new refresh token
                    var newRefreshToken = await GenerateAndSaveRefreshTokenAsync(
                        user,
                        request.IpAddress,
                        request.UserAgent);

                    // Generate new access token
                    var newAccessToken = await _authenticationService.GenerateTokenAsync(user);

                    return new AuthenticationResultDto
                    {
                        Success = true,
                        UserId = user.Id,
                        Username = user.Username,
                        AccessToken = newAccessToken,
                        RefreshToken = newRefreshToken,
                        ExpiresIn = _jwtSettings.ExpiryInMinutes * 60
                    };
                }
                else
                {
                    // Just generate new access token
                    var newAccessToken = await _authenticationService.GenerateTokenAsync(user);

                    return new AuthenticationResultDto
                    {
                        Success = true,
                        UserId = user.Id,
                        Username = user.Username,
                        AccessToken = newAccessToken,
                        RefreshToken = request.RefreshToken,
                        ExpiresIn = _jwtSettings.ExpiryInMinutes * 60
                    };
                }
            }
            catch (Exception ex) when (!(ex is UnauthorizedException))
            {
                _logger.LogError(ex, "Error refreshing token");
                throw new InvalidOperationException("An error occurred while refreshing token");
            }
        }

        #endregion

        #region Password Management

        /// <summary>
        /// Request password reset
        /// </summary>
        public async Task<bool> RequestPasswordResetAsync(PasswordResetRequestDto request)
        {
            try
            {
                // Rate limiting
                await CheckRateLimitAsync($"password_reset_{request.Email}", 3, TimeSpan.FromHours(1));

                var user = await _userService.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    // Don't reveal if user exists
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
                    return true;
                }

                // Generate reset token
                var resetToken = GenerateSecureToken();
                var resetTokenExpiry = DateTime.UtcNow.AddHours(AuthCacheKey.PASSWORD_RESET_TOKEN_EXPIRY_HOURS);

                // Save token
                user.PasswordResetToken = resetToken;
                user.PasswordResetTokenExpiry = resetTokenExpiry;
                await _userService.UpdateUserAsync(user);

                // Send reset email via event
                await PublishPasswordResetEventAsync(user, resetToken);

                // Publish event
                await _eventPublisher.PublishAsync(new PasswordResetRequestedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    IpAddress = request.IpAddress,
                    Timestamp = DateTime.UtcNow,
                    EventId = Guid.NewGuid()
                });

                _logger.LogInformation("Password reset requested for user {UserId}", user.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting password reset for {Email}", request.Email);
                throw;
            }
        }

        /// <summary>
        /// Reset password using token
        /// </summary>
        public async Task<bool> ResetPasswordAsync(ResetPasswordDto request)
        {
            try
            {
                var user = await _userRepository.Table
                    .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token);

                if (user == null)
                {
                    throw new ValidationException("Invalid reset token");
                }

                if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
                {
                    throw new ValidationException("Reset token has expired");
                }

                // Validate new password
                ValidatePassword(request.NewPassword);

                // Check password history
                if (_securitySettings.PreventPasswordReuse)
                {
                    if (_bryptPasswordServices.VerifyPassword(request.NewPassword, user.PasswordHash))
                    {
                        throw new ValidationException("New password cannot be the same as your current password");
                    }
                }

                // Update password
                user.PasswordHash = _bryptPasswordServices.HashPassword(request.NewPassword);
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpiry = null;
                user.LastPasswordChangeAt = DateTime.UtcNow;
                user.SecurityStamp = Guid.NewGuid().ToString(); // Invalidate existing tokens

                await _userService.UpdateUserAsync(user);

                // Send confirmation email via event
                await PublishPasswordChangedEventAsync(user);

                // Logout from all devices
                await LogoutFromAllDevicesAsync(user.Id);

                // Publish event
                await _eventPublisher.PublishAsync(new PasswordResetEvent
                {
                    UserId = user.Id,
                    Timestamp = DateTime.UtcNow,
                    EventId = Guid.NewGuid()
                });

                _logger.LogInformation("Password reset successfully for user {UserId}", user.Id);

                return true;
            }
            catch (Exception ex) when (!(ex is ValidationException))
            {
                _logger.LogError(ex, "Error resetting password");
                throw new InvalidOperationException("An error occurred while resetting password");
            }
        }

        /// <summary>
        /// Change password for authenticated user
        /// </summary>
        public async Task<bool> ChangePasswordAsync(ChangePasswordDto request)
        {
            try
            {
                var user = await _authenticationService.GetAuthenticatedUserAsync();
                if (user == null)
                {
                    throw new UnauthorizedException("User not authenticated");
                }

                // Verify current password
                if (!_bryptPasswordServices.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                {
                    throw new ValidationException("Current password is incorrect");
                }

                // Validate new password
                ValidatePassword(request.NewPassword);

                // Check if new password is different
                if (_bryptPasswordServices.VerifyPassword(request.NewPassword, user.PasswordHash))
                {
                    throw new ValidationException("New password must be different from current password");
                }

                // Update password
                user.PasswordHash = _bryptPasswordServices.HashPassword(request.NewPassword);
                user.LastPasswordChangeAt = DateTime.UtcNow;
                user.SecurityStamp = Guid.NewGuid().ToString();

                await _userService.UpdateUserAsync(user);

                // Send notification via event
                await PublishPasswordChangedEventAsync(user);

                // Optionally logout from other devices
                if (request.LogoutFromOtherDevices)
                {
                    await LogoutFromAllDevicesAsync(user.Id);
                }

                _logger.LogInformation("Password changed for user {UserId}", user.Id);

                return true;
            }
            catch (Exception ex) when (!(ex is ValidationException || ex is UnauthorizedException))
            {
                _logger.LogError(ex, "Error changing password");
                throw new InvalidOperationException("An error occurred while changing password");
            }
        }

        #endregion

        #region Email Verification

        /// <summary>
        /// Verify email address
        /// </summary>
        public async Task<bool> VerifyEmailAsync(VerifyEmailDto request)
        {
            try
            {
                var user = await _userRepository.Table
                    .FirstOrDefaultAsync(u => u.EmailVerificationToken == request.Token);

                if (user == null)
                {
                    throw new ValidationException("Invalid verification token");
                }

                if (user.EmailVerificationTokenExpiry < DateTime.UtcNow)
                {
                    throw new ValidationException("Verification token has expired");
                }

                if (user.EmailVerified)
                {
                    return true; // Already verified
                }

                // Update user
                user.EmailVerified = true;
                user.EmailVerifiedAt = DateTime.UtcNow;
                user.EmailVerificationToken = null;
                user.EmailVerificationTokenExpiry = null;
                user.IsActive = true;

                await _userService.UpdateUserAsync(user);

                // Send welcome email via event
                await PublishWelcomeEmailEventAsync(user);

                // Publish event
                await _eventPublisher.PublishAsync(new EmailVerifiedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Timestamp = DateTime.UtcNow,
                    EventId = Guid.NewGuid()
                });

                _logger.LogInformation("Email verified for user {UserId}", user.Id);

                return true;
            }
            catch (Exception ex) when (!(ex is ValidationException))
            {
                _logger.LogError(ex, "Error verifying email");
                throw new InvalidOperationException("An error occurred while verifying email");
            }
        }

        /// <summary>
        /// Resend verification email
        /// </summary>
        public async Task<bool> ResendVerificationEmailAsync(ResendVerificationDto request)
        {
            try
            {
                // Rate limiting
                //await CheckRateLimitAsync($"resend_verification_{request.Email}", 3, TimeSpan.FromHours(1));

                var user = await _userService.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    // Don't reveal if user exists
                    return true;
                }

                if (user.EmailVerified)
                {
                    throw new ValidationException("Email is already verified");
                }

                // Generate new token
                var verificationToken = GenerateSecureToken();
                var tokenExpiry = DateTime.UtcNow.AddDays(AuthCacheKey.EMAIL_VERIFICATION_TOKEN_EXPIRY_DAYS);

                user.EmailVerificationToken = verificationToken;
                user.EmailVerificationTokenExpiry = tokenExpiry;

                await _userService.UpdateUserAsync(user);

                // Send email via event
                await PublishEmailVerificationEventAsync(user, verificationToken);

                _logger.LogInformation("Verification email resent for user {UserId}", user.Id);

                return true;
            }
            catch (Exception ex) when (!(ex is ValidationException))
            {
                _logger.LogError(ex, "Error resending verification email");
                throw new InvalidOperationException("An error occurred while resending verification email");
            }
        }

        #endregion

        #region Two-Factor Authentication

        /// <summary>
        /// Enable two-factor authentication
        /// </summary>
        public async Task<TwoFactorSetupDto> EnableTwoFactorAsync(long userId)
        {
            try
            {
                var user = await _userService.GetByIdAsync(userId);
                if (user == null)
                {
                    throw new NotFoundException("User not found");
                }

                // Generate secret
                var secret = GenerateTwoFactorSecret();

                // Generate QR code
                var qrCodeUri = GenerateTwoFactorQrCodeUri(user.Email, secret);

                // Save secret (encrypted)
                user.TwoFactorSecret = EncryptData(secret);
                user.TwoFactorEnabled = false; // Will be enabled after verification

                await _userService.UpdateUserAsync(user);

                return new TwoFactorSetupDto
                {
                    Secret = secret,
                    QrCodeUri = qrCodeUri,
                    ManualEntryKey = FormatTwoFactorSecret(secret)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling 2FA for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Confirm two-factor authentication setup
        /// </summary>
        public async Task<bool> ConfirmTwoFactorAsync(ConfirmTwoFactorDto request)
        {
            try
            {
                var user = await _userService.GetByIdAsync(request.UserId);
                if (user == null)
                {
                    throw new NotFoundException("User not found");
                }

                // Verify code
                var secret = DecryptData(user.TwoFactorSecret);
                if (!VerifyTwoFactorCode(secret, request.Code))
                {
                    throw new ValidationException("Invalid verification code");
                }

                // Enable 2FA
                user.TwoFactorEnabled = true;
                user.TwoFactorEnabledAt = DateTime.UtcNow;

                // Generate backup codes
                var backupCodes = GenerateBackupCodes(10);
                user.TwoFactorBackupCodes = EncryptData(string.Join(",", backupCodes));

                await _userService.UpdateUserAsync(user);

                // Send confirmation email
                await PublishTwoFactorEnabledEventAsync(user, backupCodes);

                _logger.LogInformation("2FA enabled for user {UserId}", request.UserId);

                return true;
            }
            catch (Exception ex) when (!(ex is ValidationException || ex is NotFoundException))
            {
                _logger.LogError(ex, "Error confirming 2FA");
                throw new InvalidOperationException("An error occurred while confirming 2FA");
            }
        }

        /// <summary>
        /// Disable two-factor authentication
        /// </summary>
        public async Task<bool> DisableTwoFactorAsync(long userId, string password)
        {
            try
            {
                var user = await _userService.GetByIdAsync(userId);
                if (user == null || user.IsDeleted)
                    throw new NotFoundException($"User with ID {userId} not found.");

                if (!user.TwoFactorEnabled)
                    return false;

                // Verify password
                if (!_bryptPasswordServices.VerifyPassword(password, user.PasswordHash))
                    throw new UnauthorizedException("Invalid password.");

                // Disable 2FA
                user.TwoFactorEnabled = false;
                user.TwoFactorEnabledAt = null;
                user.TwoFactorSecret = null;
                user.TwoFactorBackupCodes = null;

                await _userService.UpdateUserAsync(user);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling 2FA for user {UserId}", userId);
                throw;
            }
        }

        #endregion



        #region Notification Event Publishers

        /// <summary>
        /// Publish email verification event
        /// </summary>
        private async Task PublishEmailVerificationEventAsync(User user, string token)
        {
            var verificationUrl = $"{_securitySettings.BaseUrl}/verify-email?token={token}";

            await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
            {
                EventId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = user.Id,
                To = user.Email,
                Subject = "Verify Your Email Address",
                TemplateName = "EmailVerification",
                TemplateData = new Dictionary<string, string>
  {
      { "Username", user.Username },
      { "DisplayName", user.DisplayName },
      { "VerificationUrl", verificationUrl },
      { "ExpiryHours", AuthCacheKey.EMAIL_VERIFICATION_TOKEN_EXPIRY_DAYS.ToString() }
  }
            });

            _logger.LogInformation("Email verification event published for user {UserId}", user.Id);
        }

        /// <summary>
        /// Publish password reset email event
        /// </summary>
        private async Task PublishPasswordResetEventAsync(User user, string token)
        {
            var resetUrl = $"{_securitySettings.BaseUrl}/reset-password?token={token}";

            await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
            {
                EventId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = user.Id,
                To = user.Email,
                Subject = "Password Reset Request",
                TemplateName = "PasswordReset",
                TemplateData = new Dictionary<string, string>
  {
      { "Username", user.Username },
      { "DisplayName", user.DisplayName },
      { "ResetUrl", resetUrl },
      { "ExpiryHours", AuthCacheKey.PASSWORD_RESET_TOKEN_EXPIRY_HOURS.ToString() }
  }
            });

            _logger.LogInformation("Password reset email event published for user {UserId}", user.Id);
        }

        /// <summary>
        /// Publish password changed email event
        /// </summary>
        private async Task PublishPasswordChangedEventAsync(User user)
        {
            await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
            {
                EventId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = user.Id,
                To = user.Email,
                Subject = "Your Password Has Been Changed",
                TemplateName = "PasswordChanged",
                TemplateData = new Dictionary<string, string>
  {
      { "Username", user.Username },
      { "DisplayName", user.DisplayName },
      { "ChangeDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") },
      { "SupportUrl", $"{_securitySettings.BaseUrl}/support" }
  }
            });

            _logger.LogInformation("Password changed email event published for user {UserId}", user.Id);
        }

        /// <summary>
        /// Publish welcome email event
        /// </summary>
        private async Task PublishWelcomeEmailEventAsync(User user)
        {
            await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
            {
                EventId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = user.Id,
                To = user.Email,
                Subject = $"Welcome to {_securitySettings.AppName}!",
                TemplateName = "Welcome",
                TemplateData = new Dictionary<string, string>
  {
      { "Username", user.Username },
      { "DisplayName", user.DisplayName },
      { "AppName", _securitySettings.AppName },
      { "LoginUrl", $"{_securitySettings.BaseUrl}/login" },
      { "DashboardUrl", $"{_securitySettings.BaseUrl}/dashboard" }
  }
            });

            _logger.LogInformation("Welcome email event published for user {UserId}", user.Id);
        }

        /// <summary>
        /// Publish login notification event
        /// </summary>
        private async Task PublishLoginNotificationEventAsync(User user, string ipAddress, string deviceName)
        {
            await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
            {
                EventId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = user.Id,
                To = user.Email,
                Subject = "New Login to Your Account",
                TemplateName = "LoginNotification",
                TemplateData = new Dictionary<string, string>
  {
      { "Username", user.Username },
      { "DisplayName", user.DisplayName },
      { "LoginDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") },
      { "IpAddress", ipAddress },
      { "DeviceName", deviceName },
      { "Location", await GetLocationFromIpAsync(ipAddress) }
  }
            });

            _logger.LogInformation("Login notification event published for user {UserId}", user.Id);
        }

        /// <summary>
        /// Publish 2FA enabled email event
        /// </summary>
        private async Task PublishTwoFactorEnabledEventAsync(User user, List<string> backupCodes)
        {
            await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
            {
                EventId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                UserId = user.Id,
                To = user.Email,
                Subject = "Two-Factor Authentication Enabled",
                TemplateName = "TwoFactorEnabled",
                TemplateData = new Dictionary<string, string>
  {
      { "Username", user.Username },
      { "DisplayName", user.DisplayName },
      { "EnabledDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") },
      { "BackupCodes", string.Join(", ", backupCodes) }
  }
            });

            _logger.LogInformation("2FA enabled email event published for user {UserId}", user.Id);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Generate two-factor authentication code
        /// </summary>
        private async Task<string> GenerateTwoFactorCodeAsync(User user)
        {
            try
            {
                // Generate a 6-digit code
                using var rng = RandomNumberGenerator.Create();
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var code = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 1000000;
                var formattedCode = code.ToString("D6");

                // Store the code temporarily in cache with expiration
                var cacheKey = AuthCacheKey.TwoFactorCode(user.Id);
                await _cache.SetAsync(cacheKey, formattedCode);

                _logger.LogInformation("2FA code generated for user {UserId}", user.Id);

                return formattedCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating 2FA code for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Send two-factor authentication code
        /// </summary>
        private async Task SendTwoFactorCodeAsync(User user, string code)
        {
            try
            {
                // Send via email
                if (!string.IsNullOrEmpty(user.Email))
                {
                    await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
                    {
                        EventId = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow,
                        UserId = user.Id,
                        To = user.Email,
                        Subject = "Your Two-Factor Authentication Code",
                        TemplateName = "TwoFactorCode",
                        TemplateData = new Dictionary<string, string>
              {
                  { "Username", user.Username },
                  { "DisplayName", user.DisplayName },
                  { "Code", code },
                  { "ExpiryMinutes", "5" }
              }
                    });
                }

                // Send via SMS if phone number is available
                if (!string.IsNullOrEmpty(user.PhoneNumber))
                {
                    await _eventPublisher.PublishAsync(new SendSmsNotificationEvent
                    {
                        EventId = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow,
                        UserId = user.Id,
                        PhoneNumber = user.PhoneNumber,
                        SmsBody = $"Your verification code is: {code}. This code will expire in 5 minutes."
                    });
                }

                _logger.LogInformation("2FA code sent to user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending 2FA code to user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Verify two-factor authentication code
        /// </summary>
        private async Task<bool> VerifyTwoFactorCodeAsync(User user, string code)
        {
            try
            {
                // Check if using TOTP (Time-based One-Time Password)
                if (!string.IsNullOrEmpty(user.TwoFactorSecret))
                {
                    var secret = DecryptData(user.TwoFactorSecret);
                    return VerifyTwoFactorCode(secret, code);
                }

                // Check temporary code from cache
                var cacheKey = AuthCacheKey.TwoFactorCode(user.Id);
                var storedCode = await _cache.GetAsync<string>(cacheKey);

                if (!string.IsNullOrEmpty(storedCode) && storedCode == code)
                {
                    // Remove code after successful verification
                    await _cache.RemoveAsync(cacheKey);
                    return true;
                }

                // Check backup codes
                if (!string.IsNullOrEmpty(user.TwoFactorBackupCodes))
                {
                    var backupCodes = DecryptData(user.TwoFactorBackupCodes).Split(',').ToList();
                    if (backupCodes.Contains(code))
                    {
                        // Remove used backup code
                        backupCodes.Remove(code);
                        user.TwoFactorBackupCodes = EncryptData(string.Join(",", backupCodes));
                        await _userService.UpdateUserAsync(user);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying 2FA code for user {UserId}", user.Id);
                return false;
            }
        }

        /// <summary>
        /// Create user session
        /// </summary>
        private async Task<UserSession> CreateUserSessionAsync(User user, string ipAddress, string userAgent, string refreshToken)
        {
            try
            {
                var session = new UserSession
                {
                    SessionId = Guid.NewGuid().ToString(),
                    UserId = user.Id,
                    RefreshToken = refreshToken,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    DeviceName = GetDeviceName(userAgent),
                    DeviceType = GetDeviceType(userAgent),
                    CreatedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow,
                    IsActive = true,
                    Location = await GetLocationFromIpAsync(ipAddress)
                };

                await _sessionRepository.InsertAsync(session);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Session created for user {UserId} with ID {SessionId}", user.Id, session.SessionId);

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating session for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Update last login information
        /// </summary>
        private async Task UpdateLastLoginAsync(User user, string ipAddress)
        {
            try
            {
                user.LastLoginDate = DateTime.UtcNow;
                user.LastLoginIp = ipAddress;
                user.IsOnline = true;
                user.LastSeen = DateTime.UtcNow;

                await _userService.UpdateUserAsync(user);

                _logger.LogInformation("Last login updated for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user {UserId}", user.Id);
                // Don't throw - this is not critical
            }
        }

        /// <summary>
        /// Record successful login attempt
        /// </summary>
        private async Task RecordSuccessfulLoginAsync(User user, string ipAddress)
        {
            try
            {
                var loginAttempt = new LoginAttempt
                {
                    UsernameOrEmail = user.Username,
                    IpAddress = ipAddress,
                    UserAgent = GetUserAgent(),
                    IsSuccessful = true,
                    AttemptedAt = DateTime.UtcNow,
                    UserId = user.Id,
                    Location = await GetLocationFromIpAsync(ipAddress)
                };

                await _loginAttemptRepository.InsertAsync(loginAttempt);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successful login recorded for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording successful login for user {UserId}", user.Id);
                // Don't throw - this is not critical
            }
        }

        /// <summary>
        /// Record failed login attempt
        /// </summary>
        private async Task RecordFailedLoginAttemptAsync(string usernameOrEmail, string ipAddress, string failureReason)
        {
            try
            {
                var loginAttempt = new LoginAttempt
                {
                    UsernameOrEmail = usernameOrEmail,
                    IpAddress = ipAddress,
                    UserAgent = GetUserAgent(),
                    IsSuccessful = false,
                    FailureReason = failureReason,
                    AttemptedAt = DateTime.UtcNow,
                    Location = await GetLocationFromIpAsync(ipAddress)
                };

                await _loginAttemptRepository.InsertAsync(loginAttempt);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogWarning("Failed login attempt for {UsernameOrEmail} from {IpAddress}: {Reason}",
                    usernameOrEmail, ipAddress, failureReason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording failed login attempt");
                // Don't throw - this is not critical
            }
        }

        /// <summary>
        /// Get user permissions
        /// </summary>
        private async Task<List<string>> GetUserPermissionsAsync(User user)
        {
            try
            {
                var permissions = new List<string>();

                // Add role-based permissions
                switch (user.Role)
                {
                    case UserRole.Admin:
                        permissions.AddRange(new[]
                        {
                  "users.read", "users.write", "users.delete",
                  "meetings.read", "meetings.write", "meetings.delete",
                  "settings.read", "settings.write",
                  "reports.read", "reports.write"
              });
                        break;

                    case UserRole.Moderator:
                        permissions.AddRange(new[]
                        {
                  "users.read", "users.write",
                  "meetings.read", "meetings.write", "meetings.moderate",
                  "reports.read"
              });
                        break;

                    case UserRole.Participant:
                        permissions.AddRange(new[]
                        {
                  "meetings.read", "meetings.join",
                  "profile.read", "profile.write"
              });
                        break;
                }

                return permissions.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {UserId}", user.Id);
                return new List<string>();
            }
        }

        /// <summary>
        /// End user session
        /// </summary>
        private async Task EndUserSessionAsync(string sessionId)
        {
            try
            {
                var session = await _sessionRepository.Table
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session != null && session.IsActive)
                {
                    session.IsActive = false;
                    session.EndedAt = DateTime.UtcNow;
                    await _sessionRepository.UpdateAsync(session);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Session {SessionId} ended", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending session {SessionId}", sessionId);
                // Don't throw - this is not critical
            }
        }

        /// <summary>
        /// Generate two-factor secret
        /// </summary>
        private string GenerateTwoFactorSecret()
        {
            // Generate a base32 encoded secret for TOTP
            var key = new byte[20];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }
            return Base32Encode(key);
        }

        /// <summary>
        /// Generate two-factor QR code URI
        /// </summary>
        private string GenerateTwoFactorQrCodeUri(string email, string secret)
        {
            var issuer = _securitySettings.AppName ?? "VideoConferencingApp";
            var encodedIssuer = Uri.EscapeDataString(issuer);
            var encodedEmail = Uri.EscapeDataString(email);

            return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
        }

        /// <summary>
        /// Format two-factor secret for manual entry
        /// </summary>
        private string FormatTwoFactorSecret(string secret)
        {
            var formatted = "";
            for (int i = 0; i < secret.Length; i += 4)
            {
                if (i > 0) formatted += " ";
                formatted += secret.Substring(i, Math.Min(4, secret.Length - i));
            }
            return formatted;
        }

        /// <summary>
        /// Encrypt sensitive data
        /// </summary>
        private string EncryptData(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_securitySettings.EncryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];

                using var encryptor = aes.CreateEncryptor();
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                return Convert.ToBase64String(cipherBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting data");
                throw;
            }
        }

        /// <summary>
        /// Decrypt sensitive data
        /// </summary>
        private string DecryptData(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(_securitySettings.EncryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = new byte[16];

                using var decryptor = aes.CreateDecryptor();
                var cipherBytes = Convert.FromBase64String(cipherText);
                var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting data");
                throw;
            }
        }

        /// <summary>
        /// Verify TOTP code
        /// </summary>
        private bool VerifyTwoFactorCode(string secret, string code)
        {
            try
            {
                var key = Base32Decode(secret);
                var totp = new Totp(key);

                return totp.VerifyTotp(code, out _, new VerificationWindow(1, 1));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying TOTP code");
                return false;
            }
        }

        /// <summary>
        /// Generate backup codes
        /// </summary>
        private List<string> GenerateBackupCodes(int count)
        {
            var codes = new List<string>();

            for (int i = 0; i < count; i++)
            {
                using var rng = RandomNumberGenerator.Create();
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var code = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 100000000;
                codes.Add(code.ToString("D8"));
            }

            return codes;
        }

        /// <summary>
        /// Get device name from user agent
        /// </summary>
        private string GetDeviceName(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown Device";

            if (userAgent.Contains("iPhone")) return "iPhone";
            if (userAgent.Contains("iPad")) return "iPad";
            if (userAgent.Contains("Android")) return "Android Device";
            if (userAgent.Contains("Windows")) return "Windows PC";
            if (userAgent.Contains("Mac")) return "Mac";
            if (userAgent.Contains("Linux")) return "Linux PC";

            return "Unknown Device";
        }

        /// <summary>
        /// Get device type from user agent
        /// </summary>
        private string GetDeviceType(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";

            if (userAgent.Contains("Mobile") || userAgent.Contains("Android") || userAgent.Contains("iPhone"))
                return "Mobile";
            if (userAgent.Contains("Tablet") || userAgent.Contains("iPad"))
                return "Tablet";

            return "Desktop";
        }

        /// <summary>
        /// Get location from IP address
        /// </summary>
        private async Task<string> GetLocationFromIpAsync(string ipAddress)
        {
            try
            {
                if (ipAddress == "::1" || ipAddress == "127.0.0.1")
                    return "Local";

                return "Unknown Location";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location for IP {IpAddress}", ipAddress);
                return "Unknown Location";
            }
        }

        /// <summary>
        /// Get user agent from HTTP context
        /// </summary>
        private string GetUserAgent()
        {
            return "Unknown User Agent";
        }

        /// <summary>
        /// Base32 encoding for TOTP secret
        /// </summary>
        private string Base32Encode(byte[] data)
        {
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            var result = new StringBuilder();

            int i = 0;
            while (i < data.Length)
            {
                int b1 = data[i++];
                int b2 = i < data.Length ? data[i++] : 0;
                int b3 = i < data.Length ? data[i++] : 0;
                int b4 = i < data.Length ? data[i++] : 0;
                int b5 = i < data.Length ? data[i++] : 0;

                result.Append(base32Chars[b1 >> 3]);
                result.Append(base32Chars[(b1 & 0x07) << 2 | b2 >> 6]);
                result.Append(base32Chars[b2 >> 1 & 0x1F]);
                result.Append(base32Chars[(b2 & 0x01) << 4 | b3 >> 4]);
                result.Append(base32Chars[(b3 & 0x0F) << 1 | b4 >> 7]);
                result.Append(base32Chars[b4 >> 2 & 0x1F]);
                result.Append(base32Chars[(b4 & 0x03) << 3 | b5 >> 5]);
                result.Append(base32Chars[b5 & 0x1F]);
            }

            return result.ToString().TrimEnd('A');
        }

        /// <summary>
        /// Base32 decoding for TOTP secret
        /// </summary>
        private byte[] Base32Decode(string base32)
        {
            const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            base32 = base32.ToUpper().TrimEnd('=');

            var bytes = new List<byte>();
            int buffer = 0;
            int bitsInBuffer = 0;

            foreach (char c in base32)
            {
                int value = base32Chars.IndexOf(c);
                if (value < 0)
                    throw new ArgumentException("Invalid base32 character");

                buffer = buffer << 5 | value;
                bitsInBuffer += 5;

                if (bitsInBuffer >= 8)
                {
                    bytes.Add((byte)(buffer >> bitsInBuffer - 8));
                    bitsInBuffer -= 8;
                }
            }

            return bytes.ToArray();
        }

        /// <summary>
        /// Validate registration request
        /// </summary>
        private async Task ValidateRegistrationAsync(RegisterDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length < 3 || request.Username.Length > 20)
            {
                throw new ValidationException("Username must be between 3 and 20 characters");
            }

            if (!Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_]+$"))
            {
                throw new ValidationException("Username can only contain letters, numbers, and underscores");
            }

            if (!IsValidEmail(request.Email))
            {
                throw new ValidationException("Invalid email address");
            }

            ValidatePassword(request.Password);

            var existingUser = await _userService.GetByUsernameAsync(request.Username);
            if (existingUser != null)
            {
                throw new ValidationException("Username is already taken");
            }

            existingUser = await _userService.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ValidationException("Email is already registered");
            }
        }

        /// <summary>
        /// Validate password strength
        /// </summary>
        private void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ValidationException("Password is required");
            }

            if (password.Length < _securitySettings.MinPasswordLength)
            {
                throw new ValidationException($"Password must be at least {_securitySettings.MinPasswordLength} characters");
            }

            if (_securitySettings.RequireUppercase && !password.Any(char.IsUpper))
            {
                throw new ValidationException("Password must contain at least one uppercase letter");
            }

            if (_securitySettings.RequireLowercase && !password.Any(char.IsLower))
            {
                throw new ValidationException("Password must contain at least one lowercase letter");
            }

            if (_securitySettings.RequireDigit && !password.Any(char.IsDigit))
            {
                throw new ValidationException("Password must contain at least one number");
            }

            if (_securitySettings.RequireSpecialCharacter && !Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]"))
            {
                throw new ValidationException("Password must contain at least one special character");
            }
        }



        /// <summary>
        /// Find user by username or email
        /// </summary>
        private async Task<User> FindUserByUsernameOrEmailAsync(string usernameOrEmail)
        {
            if (usernameOrEmail.Contains("@"))
            {
                return await _userService.GetByEmailAsync(usernameOrEmail);
            }
            else
            {
                return await _userService.GetByUsernameAsync(usernameOrEmail);
            }
        }

        /// <summary>
        /// Check if account is locked
        /// </summary>
        private async Task<bool> IsAccountLockedAsync(User user)
        {
            if (!user.LockoutEnabled)
                return false;

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                return true;

            return false;
        }

        /// <summary>
        /// Increment access failed count
        /// </summary>
        private async Task IncrementAccessFailedCountAsync(User user)
        {
            user.AccessFailedCount++;

            if (user.AccessFailedCount >= AuthCacheKey.MAX_LOGIN_ATTEMPTS)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(AuthCacheKey.LOCKOUT_DURATION_MINUTES);
                _logger.LogWarning("User {UserId} locked out due to {Count} failed attempts", user.Id, user.AccessFailedCount);
            }

            await _userService.UpdateUserAsync(user);
        }

        /// <summary>
        /// Reset access failed count
        /// </summary>
        private async Task ResetAccessFailedCountAsync(User user)
        {
            if (user.AccessFailedCount > 0 || user.LockoutEnd.HasValue)
            {
                user.AccessFailedCount = 0;
                user.LockoutEnd = null;
                await _userService.UpdateUserAsync(user);
            }
        }

        /// <summary>
        /// Check rate limiting using cache
        /// </summary>
        private async Task CheckRateLimitAsync(string key, int limit, TimeSpan period)
        {
            var cacheKey = AuthCacheKey.RateLimit(key, period);

            var count = await _cache.GetOrCreateAsync(
                cacheKey,
                async (cancellationToken) => 0,
                period
            );

            if (count >= limit)
            {
                throw new TooManyRequestsException($"Rate limit exceeded. Please try again later.");
            }

            await _cache.SetAsync(cacheKey, count + 1);
        }

        /// <summary>
        /// Generate secure random token
        /// </summary>
        private string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        /// <summary>
        /// Generate and save refresh token
        /// </summary>
        private async Task<string> GenerateAndSaveRefreshTokenAsync(User user, string ipAddress, string userAgent)
        {
            var refreshToken = new RefreshToken
            {
                Token = GenerateSecureToken(),
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryInDays),
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsRevoked = false
            };

            await _refreshTokenRepository.InsertAsync(refreshToken);

            return await Task.FromResult(refreshToken.Token);
        }

        /// <summary>
        /// Revoke refresh token
        /// </summary>
        private async Task RevokeRefreshTokenAsync(string token)
        {
            var refreshToken = await _refreshTokenRepository.Table
                .FirstOrDefaultAsync(rt => rt.Token == token);

            if (refreshToken != null && !refreshToken.IsRevoked)
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                await _refreshTokenRepository.UpdateAsync(refreshToken);
            }
        }

        /// <summary>
        /// Validate email format
        /// </summary>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}