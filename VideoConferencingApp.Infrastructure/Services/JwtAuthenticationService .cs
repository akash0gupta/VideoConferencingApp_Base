using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.IAuthServices;
using VideoConferencingApp.Application.Interfaces.Common.IUserServices;
using VideoConferencingApp.Domain.Entities.User;
using VideoConferencingApp.Infrastructure.Configuration;
using VideoConferencingApp.Infrastructure.Configuration.Settings;

namespace VideoConferencingApp.Infrastructure.Auth
{
    public partial class JwtAuthenticationService : IJwtAuthenticationService
    {
        #region Fields

        private readonly JwtSettings _appSettings;
        protected readonly IUserService _userService;
        protected readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<JwtAuthenticationService> _logger;

        // Thread-safe caching using AsyncLocal
        private readonly AsyncLocal<User> _cachedUser = new AsyncLocal<User>();

        #endregion

        #region Constructor

        public JwtAuthenticationService(
            AppSettings appSettings,
            IUserService userService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<JwtAuthenticationService> logger)
        {
            _appSettings = appSettings.Get<JwtSettings>();
            _userService = userService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Generate token
        /// </summary>
        /// <param name="user">User</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public Task<string> GenerateTokenAsync(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            try
            {
                // Create claims for user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("jti", Guid.NewGuid().ToString()), // JWT ID for token uniqueness
                    new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
                };
                if (!string.IsNullOrEmpty(user.Username))
                    claims.Add(new Claim(ClaimTypes.Name, user.Username, ClaimValueTypes.String, _appSettings.Issuer));
                if (!string.IsNullOrEmpty(user.Email))
                    claims.Add(new Claim(ClaimTypes.Email, user.Email, ClaimValueTypes.Email, _appSettings.Issuer));
                 claims.Add(new Claim(ClaimTypes.Role,nameof(user.Role), ClaimValueTypes.String, _appSettings.Issuer));
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.Secret));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var securityToken = new JwtSecurityToken(
                    issuer: _appSettings.Issuer,
                    audience: _appSettings.Audience,
                    claims: claims,
                    signingCredentials: credentials,
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddMinutes(_appSettings.ExpiryInMinutes)
                );

                var token = new JwtSecurityTokenHandler().WriteToken(securityToken);

                _logger.LogInformation("Token generated for user {UserId}", user.Id);

                return Task.FromResult(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating token for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Generate refresh token
        /// </summary>
        /// <param name="user">User</param>
        /// <returns>Refresh token</returns>
        public Task<string> GenerateRefreshTokenAsync(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                var refreshToken = Convert.ToBase64String(randomNumber);

                // You might want to store this refresh token in database
                // await _userService.SaveRefreshTokenAsync(user.Id, refreshToken, expiryDate);

                return Task.FromResult(refreshToken);
            }
        }

        /// <summary>
        /// Validate token
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <returns>ClaimsPrincipal if valid, null otherwise</returns>
        public Task<ClaimsPrincipal> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.Secret)),
                    ValidateIssuer = true,
                    ValidIssuer = _appSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _appSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                if (validatedToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return Task.FromResult<ClaimsPrincipal>(null);
                }

                return Task.FromResult(principal);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return Task.FromResult<ClaimsPrincipal>(null);
            }
        }

        /// <summary>
        /// Sign out
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task SignOutAsync()
        {
            try
            {
                // Reset cached customer
                _cachedUser.Value = null;

                // Check if HttpContext is available
                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext == null)
                {
                    _logger.LogWarning("HttpContext is not available for sign out");
                    return;
                }

                // Sign out from the current authentication scheme
                if (httpContext.User?.Identity?.IsAuthenticated == true)
                {
                    await httpContext.SignOutAsync(JwtBearerDefaults.AuthenticationScheme);
                    _logger.LogInformation("User signed out successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sign out");
                throw;
            }
        }

        /// <summary>
        /// Get authenticated user
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the user
        /// </returns>
        public async Task<User> GetAuthenticatedUserAsync()
        {
            // Check cached user first
            if (_cachedUser.Value != null)
                return _cachedUser.Value;

            // Check if HttpContext is available
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
            {
                _logger.LogWarning("HttpContext is not available");
                return null;
            }

            try
            {
                // Try to get authenticated user identity
                var authenticateResult = await httpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
                if (!authenticateResult.Succeeded)
                {
                    _logger.LogDebug("Authentication failed");
                    return null;
                }

                User user = null;
                var principal = authenticateResult.Principal;

                if (principal?.Claims?.Any(claim => claim.Type == ClaimTypes.NameIdentifier) == true)
                {
                    var userIdClaim = principal.FindFirst(claim =>
                        claim.Type == ClaimTypes.NameIdentifier &&
                        claim.Issuer.Equals(_appSettings.Issuer, StringComparison.InvariantCultureIgnoreCase));

                    if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
                    {
                        user = await _userService.GetByIdAsync(userId);
                    }
                }

                // Validate user
                if (user == null || !user.IsActive || user.IsDeleted)
                {
                    _logger.LogWarning("User not found or inactive: {UserId}", user?.Id);
                    return null;
                }

                // Cache authenticated user
                _cachedUser.Value = user;

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authenticated user");
                return null;
            }
        }

        /// <summary>
        /// Get current user ID from claims
        /// </summary>
        /// <returns>User ID or null</returns>
        public Task<long?> GetCurrentUserIdAsync()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return Task.FromResult<long?>(null);

            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
                return Task.FromResult<long?>(userId);

            return Task.FromResult<long?>(null);
        }

        /// <summary>
        /// Check if current user is authenticated
        /// </summary>
        /// <returns>True if authenticated</returns>
        public Task<bool> IsAuthenticatedAsync()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            var isAuthenticated = httpContext?.User?.Identity?.IsAuthenticated ?? false;
            return Task.FromResult(isAuthenticated);
        }

        /// <summary>
        /// Get user claims
        /// </summary>
        /// <returns>User claims</returns>
        public Task<IEnumerable<Claim>> GetUserClaimsAsync()
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return Task.FromResult(Enumerable.Empty<Claim>());

            return Task.FromResult(httpContext.User.Claims);
        }

        /// <summary>
        /// Check if user has specific claim
        /// </summary>
        /// <param name="claimType">Claim type</param>
        /// <param name="claimValue">Claim value</param>
        /// <returns>True if user has the claim</returns>
        public Task<bool> HasClaimAsync(string claimType, string claimValue)
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return Task.FromResult(false);

            var hasClaim = httpContext.User.HasClaim(claimType, claimValue);
            return Task.FromResult(hasClaim);
        }

        /// <summary>
        /// Check if user is in role
        /// </summary>
        /// <param name="role">Role name</param>
        /// <returns>True if user is in role</returns>
        public Task<bool> IsInRoleAsync(string role)
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return Task.FromResult(false);

            var isInRole = httpContext.User.IsInRole(role);
            return Task.FromResult(isInRole);
        }

        #endregion
    }
}
