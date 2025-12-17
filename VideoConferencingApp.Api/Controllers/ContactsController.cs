using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using VideoConferencingApp.Api.DTOs;
using VideoConferencingApp.Api.DTOs.ContactDto;
using VideoConferencingApp.API.Controllers.Base;
using VideoConferencingApp.Application.DTOs.Authentication;
using VideoConferencingApp.Application.DTOs.Contact;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Exceptions;
using VideoConferencingApp.Domain.Interfaces;
using VideoConferencingApp.Application.Services.ContactServices;
using VideoConferencingApp.Infrastructure.Services.AuthServices;

namespace VideoConferencingApp.Controllers
{
    public class ContactController : BaseController
    {
        private readonly IContactService _contactService;
        private readonly IMapper _mappingService;

        public ContactController(
            IContactService contactService,
            IMapper mappingService,
            ICurrentUserService currentUserService,
            IHttpContextService httpContextService,
            IResponseHeaderService responseHeaderService,
            ILogger<ContactController> logger)
            : base(logger, currentUserService, httpContextService, responseHeaderService)
        {
            _contactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
            _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
        }

        /// <summary>
        /// Search for users to add as contacts
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IPagedList<UserSearchDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<IPagedList<UserSearchDto>>>> SearchUsers(
            [FromQuery] string query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IPagedList<UserSearchDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (string.IsNullOrWhiteSpace(query))
                {
                    return Failure<IPagedList<UserSearchDto>>(
                        null,
                        "Search query is required.",
                        StatusCodes.Status400BadRequest);
                }

                var result = await _contactService.SearchUsersAsync(
                    CurrentUserId.Value,
                    query,
                    pageNumber,
                    pageSize);

                // Set pagination headers
                if (result != null)
                {
                    _responseHeaderService.SetPaginationHeaders(new PaginationMetadata
                    {
                        CurrentPage = pageNumber,
                        PageSize = result.PageSize,
                        TotalPages = result.TotalPages,
                        TotalCount = result.TotalCount,
                        HasNext = result.HasNextPage,
                        HasPrevious = result.HasPreviousPage
                    });
                }

                return Success(result, $"Found {result?.TotalCount ?? 0} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error searching users - Query: {Query} | UserId: {UserId} | TraceId: {TraceId}",
                    query, CurrentUserId, TraceId);

                return Failure<IPagedList<UserSearchDto>>(
                    null,
                    "An internal error occurred while searching users.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Quick search for users (autocomplete)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserQuickSearchDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserQuickSearchDto>>>> QuickSearch(
            [FromQuery] string query,
            [FromQuery] int limit = 10)
        {
            try
            {
                // Return empty list for invalid input to not break UI
                if (!IsAuthenticated || !CurrentUserId.HasValue ||
                    string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return Success(Enumerable.Empty<UserQuickSearchDto>(), "No results");
                }

                var result = await _contactService.QuickSearchAsync(
                    CurrentUserId.Value,
                    query,
                    limit);

                // Set cache control for autocomplete
                _responseHeaderService.SetCacheControl("private, max-age=60");

                return Success(result, $"Found {result?.Count() ?? 0} suggestions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in quick search - Query: {Query} | UserId: {UserId} | TraceId: {TraceId}",
                    query, CurrentUserId, TraceId);

                // Don't break autocomplete on the client
                return Success(Enumerable.Empty<UserQuickSearchDto>(), "Search unavailable");
            }
        }

        /// <summary>
        /// Get user's contacts
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IPagedList<ContactDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<IPagedList<ContactDto>>>> GetContacts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IPagedList<ContactDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var result = await _contactService.GetContactsAsync(
                    CurrentUserId.Value,
                    pageNumber,
                    pageSize);

                // Set pagination headers
                if (result != null)
                {
                    _responseHeaderService.SetPaginationHeaders(new PaginationMetadata
                    {
                        CurrentPage = pageNumber,
                        PageSize = result.PageSize,
                        TotalPages = result.TotalPages,
                        TotalCount = result.TotalCount,
                        HasNext = result.HasNextPage,
                        HasPrevious = result.HasPreviousPage
                    });
                }

                return Success(result, $"Retrieved {result?.Count ?? 0} contacts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting contacts - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<IPagedList<ContactDto>>(
                    null,
                    "An internal error occurred while getting contacts.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get pending contact requests
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ContactRequestDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<IEnumerable<ContactRequestDto>>>> GetPendingRequests(
            [FromQuery] RequestDirection direction = RequestDirection.Both)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IEnumerable<ContactRequestDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var result = await _contactService.GetPendingRequestsAsync(
                    CurrentUserId.Value,
                    direction);

                return Success(result, $"Retrieved {result?.Count() ?? 0} pending requests");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting pending requests - UserId: {UserId} | Direction: {Direction} | TraceId: {TraceId}",
                    CurrentUserId, direction, TraceId);

                return Failure<IEnumerable<ContactRequestDto>>(
                    null,
                    "An internal error occurred while getting pending requests.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Send contact request
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ContactDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<ContactDto>>> SendContactRequest(
            [FromBody] SendContactRequestDto request)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<ContactDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (!ModelState.IsValid)
                {
                    return Failure<ContactDto>(
                        null,
                        "Invalid request data.",
                        StatusCodes.Status400BadRequest);
                }

                var result = await _contactService.SendRequestAsync(
                    CurrentUserId.Value,
                    request.AddresseeId,
                    request.Message);

                _logger.LogInformation(
                    "Contact request sent - From: {UserId} To: {AddresseeId} | TraceId: {TraceId}",
                    CurrentUserId, request.AddresseeId, TraceId);

                return Success(result,"Contact request created successfully.");
            }
            catch (InvalidOperationException ex)
            {
                return Failure<ContactDto>(
                    null,
                    ex.Message,
                    StatusCodes.Status400BadRequest);
            }
            catch (NotFoundException ex)
            {
                return Failure<ContactDto>(
                    null,
                    ex.Message,
                    StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error sending contact request - UserId: {UserId} | AddresseeId: {AddresseeId} | TraceId: {TraceId}",
                    CurrentUserId, request.AddresseeId, TraceId);

                return Failure<ContactDto>(
                    null,
                    "An internal error occurred while sending the contact request.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Accept contact request
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<bool>>> AcceptRequest(
            [FromQuery] long id)
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

                var accepted = await _contactService.AcceptRequestAsync(id, CurrentUserId.Value);

                if (accepted)
                {
                    _logger.LogInformation(
                        "Contact request accepted - RequestId: {RequestId} | UserId: {UserId} | TraceId: {TraceId}",
                        id, CurrentUserId, TraceId);

                    return Success(
                        true,
                        "Contact request accepted successfully.");
                }

                return Failure<bool>(
                    false,
                    "Failed to accept contact request.",
                    StatusCodes.Status400BadRequest);
            }
            catch (NotFoundException ex)
            {
                return Failure<bool>(
                    false,
                    ex.Message,
                    StatusCodes.Status404NotFound);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<bool>(
                    false,
                    ex.Message,
                    StatusCodes.Status403Forbidden);
            }
            catch (InvalidOperationException ex)
            {
                return Failure<bool>(
                    false,
                    ex.Message,
                    StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error accepting contact request - RequestId: {RequestId} | UserId: {UserId} | TraceId: {TraceId}",
                    id, CurrentUserId, TraceId);

                return Failure<bool>(
                    false,
                    "An internal error occurred while accepting the request.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Reject contact request
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<bool>>> RejectRequest(
            [FromQuery] long id,
            [FromBody] RejectRequestDto request = null)
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

                var rejected = await _contactService.RejectRequestAsync(
                    id,
                    CurrentUserId.Value,
                    request?.Reason);

                if (rejected)
                {
                    _logger.LogInformation(
                        "Contact request rejected - RequestId: {RequestId} | UserId: {UserId} | Reason: {Reason} | TraceId: {TraceId}",
                        id, CurrentUserId, request?.Reason, TraceId);

                    return Success(
                        true,
                        "Contact request rejected.");
                }

                return Failure<bool>(
                    false,
                    "Failed to reject contact request.",
                    StatusCodes.Status400BadRequest);
            }
            catch (NotFoundException ex)
            {
                return Failure<bool>(
                    false,
                    ex.Message,
                    StatusCodes.Status404NotFound);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<bool>(
                    false,
                    ex.Message,
                    StatusCodes.Status403Forbidden);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error rejecting contact request - RequestId: {RequestId} | UserId: {UserId} | TraceId: {TraceId}",
                    id, CurrentUserId, TraceId);

                return Failure<bool>(
                    false,
                    "An internal error occurred while rejecting the request.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get single contact details
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<ContactDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<ContactDto>>> GetContact([FromQuery] long id)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<ContactDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var contact = await _contactService.GetContactByIdAsync(id, CurrentUserId.Value);

                if (contact == null)
                {
                    return Failure<ContactDto>(
                        null,
                        "Contact not found.",
                        StatusCodes.Status404NotFound);
                }

                return Success(contact, "Contact retrieved successfully.");
            }
            catch (NotFoundException ex)
            {
                return Failure<ContactDto>(
                    null,
                    ex.Message,
                    StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting contact - ContactId: {ContactId} | UserId: {UserId} | TraceId: {TraceId}",
                    id, CurrentUserId, TraceId);

                return Failure<ContactDto>(
                    null,
                    "An error occurred while getting the contact.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Cancel a sent contact request
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<bool>>> CancelRequest(
            [FromQuery] long id)
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

                var cancelled = await _contactService.CancelRequestAsync(id, CurrentUserId.Value);

                if (cancelled)
                {
                    _logger.LogInformation(
                        "Contact request cancelled - RequestId: {RequestId} | UserId: {UserId} | TraceId: {TraceId}",
                        id, CurrentUserId, TraceId);

                    return Success(
                        true,
                        "Contact request cancelled.");
                }

                return Failure<bool>(
                    false,
                    "Failed to cancel contact request.",
                    StatusCodes.Status400BadRequest);
            }
            catch (NotFoundException ex)
            {
                return Failure<bool>(
                    false,
                    ex.Message,
                    StatusCodes.Status404NotFound);
            }
            catch (UnauthorizedException ex)
            {
                return Failure<bool>(
                    false,
                    ex.Message,
                    StatusCodes.Status403Forbidden);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error cancelling contact request - RequestId: {RequestId} | UserId: {UserId} | TraceId: {TraceId}",
                    id, CurrentUserId, TraceId);

                return Failure<bool>(
                    false,
                    "An internal error occurred while cancelling the request.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Remove a contact
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> RemoveContact([FromQuery] long contactId)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<object>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var removed = await _contactService.RemoveContactAsync(CurrentUserId.Value, contactId);

                if (removed)
                {
                    _logger.LogInformation(
                        "Contact removed - ContactId: {ContactId} | UserId: {UserId} | TraceId: {TraceId}",
                        contactId, CurrentUserId, TraceId);

                    return Success<object>(null, "Contact removed successfully.");
                }

                return Failure<object>(
                    null,
                    "Failed to remove contact.",
                    StatusCodes.Status400BadRequest);
            }
            catch (NotFoundException ex)
            {
                return Failure<object>(
                    null,
                    ex.Message,
                    StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error removing contact - ContactId: {ContactId} | UserId: {UserId} | TraceId: {TraceId}",
                    contactId, CurrentUserId, TraceId);

                return Failure<object>(
                    null,
                    "An internal error occurred while removing the contact.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Block a user
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> BlockUser(
            [FromQuery] long userToBlockId,
            [FromBody] BlockUserDto request = null)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<object>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var blocked = await _contactService.BlockUserAsync(
                    CurrentUserId.Value,
                    userToBlockId,
                    request?.Reason);

                if (blocked)
                {
                    _logger.LogInformation(
                        "User blocked - BlockedUserId: {BlockedUserId} | UserId: {UserId} | TraceId: {TraceId}",
                        userToBlockId, CurrentUserId, TraceId);

                    return Success<object>(null, "User blocked successfully.");
                }

                return Failure<object>(
                    null,
                    "Failed to block user.",
                    StatusCodes.Status400BadRequest);
            }
            catch (NotFoundException ex)
            {
                return Failure<object>(
                    null,
                    ex.Message,
                    StatusCodes.Status404NotFound);
            }
            catch (InvalidOperationException ex)
            {
                return Failure<object>(
                    null,
                    ex.Message,
                    StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error blocking user - UserToBlockId: {UserToBlockId} | UserId: {UserId} | TraceId: {TraceId}",
                    userToBlockId, CurrentUserId, TraceId);

                return Failure<object>(
                    null,
                    "An internal error occurred while blocking the user.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Unblock a user
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<object>>> UnblockUser(
            [FromQuery] long userToUnblockId)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<object>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var unblocked = await _contactService.UnblockUserAsync(
                    CurrentUserId.Value,
                    userToUnblockId);

                if (unblocked)
                {
                    _logger.LogInformation(
                        "User unblocked - UnblockedUserId: {UnblockedUserId} | UserId: {UserId} | TraceId: {TraceId}",
                        userToUnblockId, CurrentUserId, TraceId);

                    return Success<object>(null, "User unblocked successfully.");
                }

                return Failure<object>(
                    null,
                    "Failed to unblock user.",
                    StatusCodes.Status400BadRequest);
            }
            catch (NotFoundException ex)
            {
                return Failure<object>(
                    null,
                    ex.Message,
                    StatusCodes.Status404NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error unblocking user - UserToUnblockId: {UserToUnblockId} | UserId: {UserId} | TraceId: {TraceId}",
                    userToUnblockId, CurrentUserId, TraceId);

                return Failure<object>(
                    null,
                    "An internal error occurred while unblocking the user.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get blocked users list
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<BlockedUserDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<IEnumerable<BlockedUserDto>>>> GetBlockedUsers()
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IEnumerable<BlockedUserDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var blockedUsers = await _contactService.GetBlockedUsersAsync(CurrentUserId.Value);

                return Success(blockedUsers, $"Retrieved {blockedUsers?.Count() ?? 0} blocked users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting blocked users - UserId: {UserId} | TraceId: {TraceId}",
                    CurrentUserId, TraceId);

                return Failure<IEnumerable<BlockedUserDto>>(
                    null,
                    "An internal error occurred while getting blocked users.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        
    }
}