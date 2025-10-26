using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VideoConferencingApp.Api.DTOs;
using VideoConferencingApp.Api.Models;
using VideoConferencingApp.Application.Interfaces;
using VideoConferencingApp.Domain.DTOs;
using VideoConferencingApp.Domain.DTOs.Contact;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Exceptions;
using VideoConferencingApp.Domain.Interfaces;

namespace VideoConferencingApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class ContactController : ControllerBase
    {
        private readonly IContactService _contactService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(
            IContactService contactService,
            ILogger<ContactController> logger)
        {
            _contactService = contactService;
            _logger = logger;
        }

        /// <summary>
        /// Search for users to add as contacts
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>List of users matching the search criteria</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IPagedList<UserSearchDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SearchUsers(
            [FromQuery] string query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new ErrorResponse
                    {
                        Message = "Search query is required",
                        Code = "INVALID_QUERY"
                    });
                }

                var result = await _contactService.SearchUsersAsync(
                    userId.Value,
                    query,
                    pageNumber,
                    pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while searching users",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Quick search for users (autocomplete)
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="limit">Maximum results</param>
        /// <returns>Quick search results</returns>
        [HttpGet("quick-search")]
        [ProducesResponseType(typeof(IEnumerable<UserQuickSearchDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> QuickSearch(
            [FromQuery] string query,
            [FromQuery] int limit = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return Ok(Enumerable.Empty<UserQuickSearchDto>());
                }

                var result = await _contactService.QuickSearchAsync(userId.Value, query, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick search");
                return Ok(Enumerable.Empty<UserQuickSearchDto>()); // Don't break autocomplete
            }
        }

        /// <summary>
        /// Get user's contacts
        /// </summary>
        /// <param name="pageNumber">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>List of user's contacts</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IPagedList<ContactDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetContacts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var result = await _contactService.GetContactsAsync(
                    userId.Value,
                    pageNumber,
                    pageSize);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contacts");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while getting contacts",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get pending contact requests
        /// </summary>
        /// <param name="direction">Request direction (sent/received/both)</param>
        /// <returns>List of pending requests</returns>
        [HttpGet("requests")]
        [ProducesResponseType(typeof(IEnumerable<ContactRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPendingRequests(
            [FromQuery] RequestDirection direction = RequestDirection.Both)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var result = await _contactService.GetPendingRequestsAsync(userId.Value, direction);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending requests");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while getting pending requests",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Send contact request
        /// </summary>
        /// <param name="request">Contact request details</param>
        /// <returns>Created contact request</returns>
        [HttpPost("request")]
        [ProducesResponseType(typeof(ContactDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SendContactRequest([FromBody] SendContactRequestDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _contactService.SendRequestAsync(
                    userId.Value,
                    request.AddresseeId,
                    request.Message);

                return CreatedAtAction(
                    nameof(GetContact),
                    new { id = result.Id },
                    result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "INVALID_REQUEST"
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "NOT_FOUND"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending contact request");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while sending contact request",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Accept contact request
        /// </summary>
        /// <param name="id">Contact request ID</param>
        /// <returns>Success confirmation</returns>
        [HttpPost("{id}/accept")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> AcceptRequest(long id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var result = await _contactService.AcceptRequestAsync(id, userId.Value);

                if (result)
                {
                    return Ok(new { message = "Contact request accepted successfully" });
                }

                return BadRequest(new ErrorResponse
                {
                    Message = "Failed to accept contact request",
                    Code = "ACCEPT_FAILED"
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "NOT_FOUND"
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "INVALID_OPERATION"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting contact request");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while accepting contact request",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Reject contact request
        /// </summary>
        /// <param name="id">Contact request ID</param>
        /// <param name="request">Rejection details</param>
        /// <returns>Success confirmation</returns>
        [HttpPost("{id}/reject")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RejectRequest(long id, [FromBody] RejectRequestDto request = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var result = await _contactService.RejectRequestAsync(
                    id,
                    userId.Value,
                    request?.Reason);

                if (result)
                {
                    return Ok(new { message = "Contact request rejected" });
                }

                return BadRequest(new ErrorResponse
                {
                    Message = "Failed to reject contact request",
                    Code = "REJECT_FAILED"
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "NOT_FOUND"
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
                _logger.LogError(ex, "Error rejecting contact request");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while rejecting contact request",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Cancel sent contact request
        /// </summary>
        /// <param name="id">Contact request ID</param>
        /// <returns>Success confirmation</returns>
        [HttpDelete("request/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CancelRequest(long id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var result = await _contactService.CancelRequestAsync(id, userId.Value);

                if (result)
                {
                    return Ok(new { message = "Contact request cancelled" });
                }

                return BadRequest(new ErrorResponse
                {
                    Message = "Failed to cancel contact request",
                    Code = "CANCEL_FAILED"
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "NOT_FOUND"
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
                _logger.LogError(ex, "Error cancelling contact request");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while cancelling contact request",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Remove contact
        /// </summary>
        /// <param name="id">Contact ID</param>
        /// <returns>Success confirmation</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RemoveContact(long id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var result = await _contactService.RemoveContactAsync(id, userId.Value);

                if (result)
                {
                    return Ok(new { message = "Contact removed successfully" });
                }

                return BadRequest(new ErrorResponse
                {
                    Message = "Failed to remove contact",
                    Code = "REMOVE_FAILED"
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "NOT_FOUND"
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
                _logger.LogError(ex, "Error removing contact");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while removing contact",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get single contact details
        /// </summary>
        /// <param name="id">Contact ID</param>
        /// <returns>Contact details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetContact(long id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                // Implementation would fetch specific contact
                // This is a placeholder
                return Ok(new ContactDto { Id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contact");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while getting contact",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Block a user
        /// </summary>
        /// <param name="request">Block request details</param>
        /// <returns>Success confirmation</returns>
        [HttpPost("block")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> BlockUser([FromBody] BlockUserDto request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _contactService.BlockUserAsync(
                    userId.Value,
                    request.UserToBlockId,
                    request.Reason);

                if (result)
                {
                    return Ok(new { message = "User blocked successfully" });
                }

                return BadRequest(new ErrorResponse
                {
                    Message = "Failed to block user",
                    Code = "BLOCK_FAILED"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "INVALID_OPERATION"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking user");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while blocking user",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Unblock a user
        /// </summary>
        /// <param name="userToUnblockId">User ID to unblock</param>
        /// <returns>Success confirmation</returns>
        [HttpDelete("block/{userToUnblockId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UnblockUser(long userToUnblockId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var result = await _contactService.UnblockUserAsync(userId.Value, userToUnblockId);

                if (result)
                {
                    return Ok(new { message = "User unblocked successfully" });
                }

                return BadRequest(new ErrorResponse
                {
                    Message = "Failed to unblock user",
                    Code = "UNBLOCK_FAILED"
                });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new ErrorResponse
                {
                    Message = ex.Message,
                    Code = "NOT_FOUND"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking user");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while unblocking user",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        /// <summary>
        /// Get blocked users
        /// </summary>
        /// <returns>List of blocked users</returns>
        [HttpGet("blocked")]
        [ProducesResponseType(typeof(IEnumerable<BlockedUserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetBlockedUsers()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var result = await _contactService.GetBlockedUsersAsync(userId.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blocked users");
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
                {
                    Message = "An error occurred while getting blocked users",
                    Code = "INTERNAL_ERROR"
                });
            }
        }

        #region Helper Methods

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