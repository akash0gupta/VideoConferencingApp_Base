using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.DTOs.Contact;
using VideoConferencingApp.Domain.Entities;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Interfaces;

namespace VideoConferencingApp.Application.Services.ContactServices
{
    public interface IContactService
    {
        #region User Search

        /// <summary>
        /// Search users with pagination and filtering
        /// </summary>
        Task<IPagedList<UserSearchDto>> SearchUsersAsync(
            long currentUserId,
            string query,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Quick search with auto-complete support
        /// </summary>
        Task<IEnumerable<UserQuickSearchDto>> QuickSearchAsync(
            long currentUserId,
            string query,
            int limit = 10);

        #endregion

        #region Contact Requests

        /// <summary>
        /// Send a contact request with validation
        /// </summary>
        Task<ContactDto> SendRequestAsync(
            long requesterId,
            long addresseeId,
            string message = null);

        /// <summary>
        /// Accept a contact request
        /// </summary>
        Task<bool> AcceptRequestAsync(
            long contactId,
            long currentUserId);

        /// <summary>
        /// Reject a contact request
        /// </summary>
        Task<bool> RejectRequestAsync(
            long contactId,
            long currentUserId,
            string reason = null);

        /// <summary>
        /// Cancel a sent contact request
        /// </summary>
        Task<bool> CancelRequestAsync(
            long contactId,
            long currentUserId);

        #endregion

        #region Contact Management

        /// <summary>
        /// Get user's contacts with filtering and pagination
        /// </summary>
        Task<IPagedList<ContactDto>> GetContactsAsync(
            long userId,
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>
        /// Get user's contacts by id
        /// </summary>
        Task<ContactDto> GetContactByIdAsync(long contactId, long currentUserId);

        /// <summary>
        /// Get pending contact requests (sent, received, or both)
        /// </summary>
        Task<IEnumerable<ContactRequestDto>> GetPendingRequestsAsync(
            long userId,
            RequestDirection direction = RequestDirection.Both);

        /// <summary>
        /// Remove a contact
        /// </summary>
        Task<bool> RemoveContactAsync(
            long contactId,
            long currentUserId);

        /// <summary>
        /// Block a user
        /// </summary>
        Task<bool> BlockUserAsync(
            long userId,
            long userToBlockId,
            string reason = null);

        /// <summary>
        /// Unblock a user
        /// </summary>
        Task<bool> UnblockUserAsync(
            long userId,
            long userToUnblockId);

        /// <summary>
        /// Get all users blocked by the current user
        /// </summary>
        Task<IEnumerable<BlockedUserDto>> GetBlockedUsersAsync(
            long userId);

        #endregion
    }
}
