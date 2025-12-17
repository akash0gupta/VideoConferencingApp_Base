using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoConferencingApp.Api.DTOs;
using VideoConferencingApp.Infrastructure.Services.AuthServices;
using static LinqToDB.Common.Configuration;

namespace VideoConferencingApp.API.Controllers.Base
{

        [ApiController]
        [ApiVersion("1")]
        [Route("api/v{v:apiVersion}/[controller]/[action]")]
        [Authorize]
        [Produces("application/json")]
        public abstract class BaseController : ControllerBase
        {
            protected readonly ILogger _logger;
            protected readonly ICurrentUserService _currentUserService;
            protected readonly IHttpContextService _httpContextService;
            protected readonly IResponseHeaderService _responseHeaderService;

            protected BaseController(
                ILogger logger,
                ICurrentUserService currentUserService,
                IHttpContextService httpContextService,
                IResponseHeaderService responseHeaderService)
            {
                _logger = logger;
                _currentUserService = currentUserService;
                _httpContextService = httpContextService;
                _responseHeaderService = responseHeaderService;
            }

            #region Response Methods

            protected ActionResult<ApiResponse<T>> Success<T>(T result, string? message = null)
            {
                return Ok(new ApiResponse<T>
                {
                    Success = true,
                    Message = message ?? "Operation successful",
                    Data = result,
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }

            protected ActionResult<ApiResponse<object>> Success(string? message = null)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = message ?? "Operation successful",
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }

            protected ActionResult<ApiResponse<T>> Failure<T>(T result, string errorMessage, int statusCode = 400)
            {
                _logger.LogWarning("API Error: {Message} | Status: {StatusCode} | TraceId: {TraceId} | User: {UserId}",
                    errorMessage, statusCode, TraceId, CurrentUserId);

                return StatusCode(statusCode, new ApiResponse<T>
                {
                    Success = false,
                    Message = errorMessage,
                    Data = result,
                    TraceId = TraceId,
                    Timestamp = DateTime.UtcNow
                });
            }


            #endregion

            #region Helper Properties

            /// <summary>
            /// Gets current trace identifier
            /// </summary>
            protected string TraceId => _httpContextService.TraceId;

            /// <summary>
            /// Gets correlation identifier
            /// </summary>
            protected string CorrelationId => _httpContextService.CorrelationId;

            /// <summary>
            /// Gets current user ID
            /// </summary>
            protected long? CurrentUserId => _currentUserService.UserId;

            /// <summary>
            /// Gets current username
            /// </summary>
            protected string? CurrentUsername => _currentUserService.Username;

            /// <summary>
            /// Gets current user email
            /// </summary>
            protected string? CurrentUserEmail => _currentUserService.Email;

            /// <summary>
            /// Gets current user role
            /// </summary>
            protected string? CurrentUserRole => _currentUserService.Role;

            /// <summary>
            /// Gets all user roles
            /// </summary>
            protected IEnumerable<string> CurrentUserRoles => _currentUserService.Roles;

            /// <summary>
            /// Checks if user is authenticated
            /// </summary>
            protected bool IsAuthenticated => _currentUserService.IsAuthenticated;

            /// <summary>
            /// Gets client IP address
            /// </summary>
            protected string ClientV6IpAddress => _httpContextService.IpAddressV6;
            protected string ClientV4IpAddress => _httpContextService.IpAddressV4;

            /// <summary>
            /// Gets user agent
            /// </summary>
            protected string UserAgent => _httpContextService.UserAgent;

            #endregion

            #region Helper Methods

            /// <summary>
            /// Check if user is in specific role
            /// </summary>
            protected bool IsInRole(string role) => _currentUserService.IsInRole(role);

            /// <summary>
            /// Set refresh token cookie
            /// </summary>
            protected void SetRefreshTokenCookie(string refreshToken, int expirationDays = 7)
            {
                var options = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(expirationDays)
                };

                _httpContextService.SetCookie("refreshToken", refreshToken, options);
            }

            /// <summary>
            /// Get refresh token from cookie
            /// </summary>
            protected string? GetRefreshTokenFromCookie()
            {
                return _httpContextService.GetCookie("refreshToken");
            }

            /// <summary>
            /// Delete refresh token cookie
            /// </summary>
            protected void DeleteRefreshTokenCookie()
            {
                _httpContextService.DeleteCookie("refreshToken");
            }

            #endregion
        }
    
}