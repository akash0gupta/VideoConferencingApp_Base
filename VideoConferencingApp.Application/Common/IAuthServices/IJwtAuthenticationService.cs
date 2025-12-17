using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Entities.UserEntities;

namespace VideoConferencingApp.Application.Common.IAuthServices
{
    /// <summary>
    /// Authentication service interface
    /// </summary>
    public interface IJwtAuthenticationService
    {
        #region Token Management

        /// <summary>
        /// Generate JWT token for user
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>JWT token string</returns>
        Task<string> GenerateTokenAsync(User user);

        /// <summary>
        /// Generate refresh token for user
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>Refresh token string</returns>
        Task<string> GenerateRefreshTokenAsync(User user);

        /// <summary>
        /// Validate JWT token
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <returns>ClaimsPrincipal if valid, null otherwise</returns>
        Task<ClaimsPrincipal> ValidateTokenAsync(string token);

        #endregion

        #region User Authentication

        /// <summary>
        /// Get currently authenticated user
        /// </summary>
        /// <returns>User entity or null if not authenticated</returns>
        Task<User> GetAuthenticatedUserAsync();

        /// <summary>
        /// Get current user ID from claims
        /// </summary>
        /// <returns>User ID or null if not authenticated</returns>
        Task<long?> GetCurrentUserIdAsync();

        /// <summary>
        /// Check if current request is authenticated
        /// </summary>
        /// <returns>True if authenticated, false otherwise</returns>
        Task<bool> IsAuthenticatedAsync();

        /// <summary>
        /// Sign out current user
        /// </summary>
        /// <returns>Task</returns>
        Task SignOutAsync();

        #endregion

        #region Claims Management

        /// <summary>
        /// Get all claims for current user
        /// </summary>
        /// <returns>Collection of user claims</returns>
        Task<IEnumerable<Claim>> GetUserClaimsAsync();

        /// <summary>
        /// Check if user has specific claim
        /// </summary>
        /// <param name="claimType">Type of claim</param>
        /// <param name="claimValue">Value of claim</param>
        /// <returns>True if user has the claim</returns>
        Task<bool> HasClaimAsync(string claimType, string claimValue);

        #endregion

        #region Role Management

        /// <summary>
        /// Check if user is in specific role
        /// </summary>
        /// <param name="role">Role name</param>
        /// <returns>True if user is in role</returns>
        Task<bool> IsInRoleAsync(string role);

        #endregion
    }
}
