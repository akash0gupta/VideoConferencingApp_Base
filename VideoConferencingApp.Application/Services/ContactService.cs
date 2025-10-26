using LinqToDB;
using LinqToDB.Common.Internal.Cache;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Transactions;
using VideoConferencingApp.Application.Events;
using VideoConferencingApp.Application.Interfaces;
using VideoConferencingApp.Application.Interfaces.Common.IAuthServices;
using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;
using VideoConferencingApp.Application.Interfaces.Common.IEventHandlerServices;
using VideoConferencingApp.Domain.CacheKeys;
using VideoConferencingApp.Domain.DTOs.Common;
using VideoConferencingApp.Domain.DTOs.Contact;
using VideoConferencingApp.Domain.DTOs.Notification;
using VideoConferencingApp.Domain.Entities;
using VideoConferencingApp.Domain.Entities.User;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Events.ContactEvents;
using VideoConferencingApp.Domain.Events.Notification;
using VideoConferencingApp.Domain.Exceptions;
using VideoConferencingApp.Domain.Interfaces;
using VideoConferencingApp.Domain.Models;

namespace VideoConferencingApp.Services
{
    public class ContactService : IContactService
    {
        #region Fields

        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Contact> _contactRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<ContactService> _logger;
        private readonly IStaticCacheManager _cache;
        private readonly IJwtAuthenticationService _authenticationService;
        private readonly IUnitOfWork _unitOfWork;

        // Cache key prefixes
        private const string CONTACTS_CACHE_KEY = "contacts_user_{0}";
        private const string PENDING_REQUESTS_CACHE_KEY = "pending_requests_user_{0}";
        private const string BLOCKED_USERS_CACHE_KEY = "blocked_users_{0}";

        #endregion

        #region Constructor

        public ContactService(
            IRepository<User> userRepository,
            IRepository<Contact> contactRepository,
            IEventPublisher eventPublisher,
            ILogger<ContactService> logger,
            IStaticCacheManager cache,
            IJwtAuthenticationService authenticationService,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _contactRepository = contactRepository ?? throw new ArgumentNullException(nameof(contactRepository));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        #endregion

        #region User Search

        /// <summary>
        /// Search users with pagination and filtering
        /// </summary>
        public async Task<IPagedList<UserSearchDto>> SearchUsersAsync(
            long currentUserId,
            string query,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                var normalizedQuery = query?.ToLower().Trim();

                var usersQuery = _userRepository.Table
                    .Where(u => u.IsActive && !u.IsDeleted && u.Id != currentUserId);

                if (!string.IsNullOrWhiteSpace(normalizedQuery))
                {
                    usersQuery = usersQuery.Where(u =>
                        u.Username.ToLower().Contains(normalizedQuery) ||
                        u.Email.ToLower().Contains(normalizedQuery) ||
                        u.DisplayName.ToLower().Contains(normalizedQuery));
                }

                var totalCount = await usersQuery.CountAsync();
                var users = await usersQuery
                    .OrderBy(u => u.Username)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get existing contact relationships
                var userIds = users.Select(u => u.Id);
                var contactsMap = await GetExistingContactsMapAsync(currentUserId, userIds);

                var userDtos = users.Select(u => MapToUserSearchDto(u, contactsMap)).ToList();

                return new PagedList<UserSearchDto>(userDtos, pageNumber, pageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with query {Query}", query);
                throw;
            }
        }

        /// <summary>
        /// Quick search with auto-complete support
        /// </summary>
        public async Task<IEnumerable<UserQuickSearchDto>> QuickSearchAsync(long currentUserId, string query, int limit = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                    return Enumerable.Empty<UserQuickSearchDto>();

                var normalizedQuery = query.ToLower().Trim();

                var users = await _userRepository.Table
                    .Where(u => u.IsActive && !u.IsDeleted && u.Id != currentUserId &&
                        (u.Username.ToLower().Contains(normalizedQuery) ||
                         u.DisplayName.ToLower().Contains(normalizedQuery)))
                    .OrderBy(u => u.Username)
                    .Take(limit)
                    .Select(u => new UserQuickSearchDto
                    {
                        Id = u.Id,
                        Username = u.Username,
                        DisplayName = u.DisplayName,
                        ProfilePictureUrl = u.ProfilePictureUrl,
                        IsOnline = u.IsOnline
                    })
                    .ToListAsync();

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick search with query {Query}", query);
                throw;
            }
        }

        #endregion

        #region Contact Requests

        /// <summary>
        /// Send contact request with validation
        /// </summary>
        public async Task<ContactDto> SendRequestAsync(long requesterId, long addresseeId, string message = null)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Validate users
                await ValidateContactRequestAsync(requesterId, addresseeId);

                // Get user details for the contact
                var requester = await _userRepository.GetByIdAsync(requesterId);
                var addressee = await _userRepository.GetByIdAsync(addresseeId);

                if (requester == null || addressee == null)
                {
                    throw new NotFoundException("User not found");
                }

                // Create contact request
                var contact = new Contact
                {
                    RequesterId = requesterId,
                    AddresseeId = addresseeId,
                    RequesterUserName = requester.Username,
                    AddresseeUserName = addressee.Username,
                    Status = ContactStatus.Pending,
                    Message = message,
                    RequestedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _contactRepository.InsertAsync(contact);
                await _unitOfWork.SaveChangesAsync();

                // Clear cache
                await InvalidateContactsCacheAsync(requesterId, addresseeId);

                // Send notifications via events
                await PublishContactRequestNotificationsAsync(contact, requester, addressee);

                // Publish domain event
                await _eventPublisher.PublishAsync(new ContactRequestSentEvent
                {
                    Contact = contact,
                    RequesterName = requester.DisplayName ?? requester.Username,
                    AddresseeName = addressee.DisplayName ?? addressee.Username,
                    AddresseeEmail = addressee.Email,
                    RequesterEmail = requester.Email,
                    AddresseePhoneNumber = addressee.PhoneNumber,
                    Timestamp = DateTime.UtcNow,
                    EventId = Guid.NewGuid()
                });

                await transaction.CommitAsync();

                _logger.LogInformation("Contact request sent from {RequesterId} to {AddresseeId}", requesterId, addresseeId);

                return MapToContactDto(contact, requester, addressee);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error sending contact request from {RequesterId} to {AddresseeId}", requesterId, addresseeId);
                throw;
            }
        }

        /// <summary>
        /// Accept contact request
        /// </summary>
        public async Task<bool> AcceptRequestAsync(long contactId, long currentUserId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                var contact = await _contactRepository.GetByIdAsync(contactId);

                if (contact == null)
                {
                    throw new NotFoundException($"Contact request {contactId} not found");
                }

                if (contact.AddresseeId != currentUserId)
                {
                    throw new UnauthorizedException("You are not authorized to accept this request");
                }

                if (contact.Status != ContactStatus.Pending)
                {
                    throw new InvalidOperationException($"Contact request is already {contact.Status}");
                }

                // Update status
                contact.Status = ContactStatus.Accepted;
                contact.AcceptedAt = DateTime.UtcNow;
                contact.UpdatedAt = DateTime.UtcNow;

                await _contactRepository.UpdateAsync(contact);
                await _unitOfWork.SaveChangesAsync();

                // Clear cache
                await InvalidateContactsCacheAsync(contact.RequesterId, contact.AddresseeId);

                // Get user details for notifications
                var requester = await _userRepository.GetByIdAsync(contact.RequesterId);
                var addressee = await _userRepository.GetByIdAsync(contact.AddresseeId);

                // Send notifications via events
                await PublishContactAcceptedNotificationsAsync(contact, requester, addressee);

                // Publish domain event
                await _eventPublisher.PublishAsync(new ContactRequestAcceptedEvent
                {
                    Contact = contact,
                    RequesterName = requester.DisplayName ?? requester.Username,
                    AddresseeName = addressee.DisplayName ?? addressee.Username,
                    RequesterEmail = requester.Email,
                    AddresseeEmail = addressee.Email,
                    RequesterPhoneNumber = requester.PhoneNumber,
                    Timestamp = DateTime.UtcNow,
                    EventId = Guid.NewGuid()
                });

                await transaction.CommitAsync();

                _logger.LogInformation("Contact request {ContactId} accepted by user {UserId}", contactId, currentUserId);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error accepting contact request {ContactId}", contactId);
                throw;
            }
        }

        /// <summary>
        /// Reject contact request
        /// </summary>
        public async Task<bool> RejectRequestAsync(long contactId, long currentUserId, string reason = null)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                var contact = await _contactRepository.GetByIdAsync(contactId);

                if (contact == null)
                {
                    throw new NotFoundException($"Contact request {contactId} not found");
                }

                if (contact.AddresseeId != currentUserId)
                {
                    throw new UnauthorizedException("You are not authorized to reject this request");
                }

                if (contact.Status != ContactStatus.Pending)
                {
                    throw new InvalidOperationException($"Contact request is already {contact.Status}");
                }

                // Update status
                contact.Status = ContactStatus.Rejected;
                contact.RejectedAt = DateTime.UtcNow;
                contact.RejectionReason = reason;
                contact.UpdatedAt = DateTime.UtcNow;

                await _contactRepository.UpdateAsync(contact);
                await _unitOfWork.SaveChangesAsync();

                // Clear cache
                await InvalidateContactsCacheAsync(contact.RequesterId, contact.AddresseeId);

                // Get user details
                var requester = await _userRepository.GetByIdAsync(contact.RequesterId);
                var addressee = await _userRepository.GetByIdAsync(contact.AddresseeId);

                // Send notification to requester
                await PublishContactRejectedNotificationsAsync(contact, requester, addressee, reason);

                await transaction.CommitAsync();

                _logger.LogInformation("Contact request {ContactId} rejected by user {UserId}", contactId, currentUserId);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error rejecting contact request {ContactId}", contactId);
                throw;
            }
        }

        /// <summary>
        /// Cancel sent contact request
        /// </summary>
        public async Task<bool> CancelRequestAsync(long contactId, long currentUserId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                var contact = await _contactRepository.GetByIdAsync(contactId);

                if (contact == null)
                {
                    throw new NotFoundException($"Contact request {contactId} not found");
                }

                if (contact.RequesterId != currentUserId)
                {
                    throw new UnauthorizedException("You are not authorized to cancel this request");
                }

                if (contact.Status != ContactStatus.Pending)
                {
                    throw new InvalidOperationException("Can only cancel pending requests");
                }

                // Delete the request
                await _contactRepository.DeleteAsync(contact);
                await _unitOfWork.SaveChangesAsync();

                // Clear cache
                await InvalidateContactsCacheAsync(contact.RequesterId, contact.AddresseeId);

                await transaction.CommitAsync();

                _logger.LogInformation("Contact request {ContactId} cancelled by user {UserId}", contactId, currentUserId);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling contact request {ContactId}", contactId);
                throw;
            }
        }

        #endregion

        #region Contact Management

        /// <summary>
        /// Get user's contacts with filtering and pagination
        /// </summary>
        public async Task<IPagedList<ContactDto>> GetContactsAsync(
            long userId,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                var cacheKey = new CacheKey(string.Format(CONTACTS_CACHE_KEY, userId));

                var contactsQuery = _contactRepository.Table
                    .Where(c => (c.RequesterId == userId || c.AddresseeId == userId) &&
                               c.Status == ContactStatus.Accepted &&
                               c.IsActive &&
                               !c.IsDeleted);

                var totalCount = await contactsQuery.CountAsync();

                var contacts = await contactsQuery
                    .OrderByDescending(c => c.AcceptedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (!contacts.Any())
                    return new PagedList<ContactDto>(new List<ContactDto>(), pageNumber, pageSize, 0);

                // Get all related user IDs
                var userIds = contacts.SelectMany(c => new[] { c.RequesterId, c.AddresseeId }).Distinct();
                var users = await _userRepository.FindAsync(u => userIds.Contains(u.Id));
                var userDict = users.ToDictionary(u => u.Id);

                var contactDtos = contacts.Select(c =>
                {
                    var requester = userDict.GetValueOrDefault(c.RequesterId);
                    var addressee = userDict.GetValueOrDefault(c.AddresseeId);
                    return MapToContactDto(c, requester, addressee, userId);
                }).ToList();

                return new PagedList<ContactDto>(contactDtos, pageNumber, pageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contacts for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Get pending contact requests
        /// </summary>
        public async Task<IEnumerable<ContactRequestDto>> GetPendingRequestsAsync(long userId, RequestDirection direction = RequestDirection.Both)
        {
            try
            {
                var query = _contactRepository.Table
                    .Where(c => c.Status == ContactStatus.Pending);

                query = direction switch
                {
                    RequestDirection.Sent => query.Where(c => c.RequesterId == userId),
                    RequestDirection.Received => query.Where(c => c.AddresseeId == userId),
                    _ => query.Where(c => c.RequesterId == userId || c.AddresseeId == userId)
                };

                var requests = await query
                    .OrderByDescending(c => c.RequestedAt)
                    .ToListAsync();

                // Get user details
                var userIds = requests.SelectMany(c => new[] { c.RequesterId, c.AddresseeId }).Distinct();
                var users = await _userRepository.FindAsync(u => userIds.Contains(u.Id));
                var userDict = users.ToDictionary(u => u.Id);

                return requests.Select(r => MapToContactRequestDto(r, userDict, userId)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending requests for user {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// Remove contact
        /// </summary>
        public async Task<bool> RemoveContactAsync(long contactId, long currentUserId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                var contact = await _contactRepository.GetByIdAsync(contactId);

                if (contact == null)
                {
                    throw new NotFoundException($"Contact {contactId} not found");
                }

                if (contact.RequesterId != currentUserId && contact.AddresseeId != currentUserId)
                {
                    throw new UnauthorizedException("You are not authorized to remove this contact");
                }

                if (contact.Status != ContactStatus.Accepted)
                {
                    throw new InvalidOperationException("Can only remove accepted contacts");
                }

                // Soft delete
                contact.IsActive = false;
                contact.IsDeleted = true;
                contact.DeletedAt = DateTime.UtcNow;
                contact.DeletedBy = currentUserId;

                await _contactRepository.UpdateAsync(contact);
                await _unitOfWork.SaveChangesAsync();

                // Clear cache
                await InvalidateContactsCacheAsync(contact.RequesterId, contact.AddresseeId);

                // Get user details
                var currentUser = await _userRepository.GetByIdAsync(currentUserId);
                var otherUserId = contact.RequesterId == currentUserId ? contact.AddresseeId : contact.RequesterId;
                var otherUser = await _userRepository.GetByIdAsync(otherUserId);

                // Notify the other user via Push notification
                await _eventPublisher.PublishAsync(new SendPushNotificationEvent
                {
                    EventId = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    UserId = otherUserId.ToString(),
                    Target = NotificationTarget.User,
                    TargetId = otherUserId.ToString(),
                    Method = "ContactRemoved",
                    Payload = new
                    {
                        contactId,
                        removedByUserId = currentUserId,
                        removedByUsername = currentUser?.Username,
                        removedByDisplayName = currentUser?.DisplayName,
                        message = "A contact has been removed from your list"
                    }
                });

                await transaction.CommitAsync();

                _logger.LogInformation("Contact {ContactId} removed by user {UserId}", contactId, currentUserId);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error removing contact {ContactId}", contactId);
                throw;
            }
        }

        /// <summary>
        /// Block a user
        /// </summary>
        public async Task<bool> BlockUserAsync(long userId, long userToBlockId, string reason = null)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (userId == userToBlockId)
                {
                    throw new InvalidOperationException("Cannot block yourself");
                }

                // Check if already blocked
                var existingBlock = await _contactRepository.Table
                    .FirstOrDefaultAsync(c =>
                        c.RequesterId == userId &&
                        c.AddresseeId == userToBlockId &&
                        c.Status == ContactStatus.Blocked);

                if (existingBlock != null)
                {
                    throw new InvalidOperationException("User is already blocked");
                }

                // Remove any existing contact
                var existingContact = await _contactRepository.Table
                    .FirstOrDefaultAsync(c =>
                        ((c.RequesterId == userId && c.AddresseeId == userToBlockId) ||
                         (c.RequesterId == userToBlockId && c.AddresseeId == userId)) &&
                        c.Status != ContactStatus.Blocked);

                if (existingContact != null)
                {
                    existingContact.IsActive = false;
                    existingContact.IsDeleted = true;
                    await _contactRepository.UpdateAsync(existingContact);
                }

                // Create block entry
                var blockEntry = new Contact
                {
                    RequesterId = userId,
                    AddresseeId = userToBlockId,
                    Status = ContactStatus.Blocked,
                    BlockedAt = DateTime.UtcNow,
                    BlockReason = reason,
                    IsActive = true
                };

                await _contactRepository.InsertAsync(blockEntry);
                await _unitOfWork.SaveChangesAsync();

                // Clear cache
                await InvalidateContactsCacheAsync(userId, userToBlockId);

                await transaction.CommitAsync();

                _logger.LogInformation("User {UserId} blocked user {BlockedUserId}", userId, userToBlockId);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error blocking user {UserToBlockId} by user {UserId}", userToBlockId, userId);
                throw;
            }
        }

        /// <summary>
        /// Unblock a user
        /// </summary>
        public async Task<bool> UnblockUserAsync(long userId, long userToUnblockId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                var blockEntry = await _contactRepository.Table
                    .FirstOrDefaultAsync(c =>
                        c.RequesterId == userId &&
                        c.AddresseeId == userToUnblockId &&
                        c.Status == ContactStatus.Blocked);

                if (blockEntry == null)
                {
                    throw new NotFoundException("Block entry not found");
                }

                // Remove block entry
                await _contactRepository.DeleteAsync(blockEntry);
                await _unitOfWork.SaveChangesAsync();

                // Clear cache
                await InvalidateContactsCacheAsync(userId, userToUnblockId);

                await transaction.CommitAsync();

                _logger.LogInformation("User {UserId} unblocked user {UnblockedUserId}", userId, userToUnblockId);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error unblocking user {UserToUnblockId} by user {UserId}", userToUnblockId, userId);
                throw;
            }
        }

        /// <summary>
        /// Get blocked users
        /// </summary>
        public async Task<IEnumerable<BlockedUserDto>> GetBlockedUsersAsync(long userId)
        {
            try
            {
                var blockedContacts = await _contactRepository.Table
                    .Where(c => c.RequesterId == userId && c.Status == ContactStatus.Blocked)
                    .OrderByDescending(c => c.BlockedAt)
                    .ToListAsync();

                if (!blockedContacts.Any())
                    return Enumerable.Empty<BlockedUserDto>();

                var blockedUserIds = blockedContacts.Select(c => c.AddresseeId);
                var users = await _userRepository.FindAsync(u => blockedUserIds.Contains(u.Id));
                var userDict = users.ToDictionary(u => u.Id);

                return blockedContacts.Select(c =>
                {
                    var user = userDict.GetValueOrDefault(c.AddresseeId);
                    return new BlockedUserDto
                    {
                        UserId = c.AddresseeId,
                        Username = user?.Username,
                        DisplayName = user?.DisplayName,
                        ProfilePictureUrl = user?.ProfilePictureUrl,
                        BlockedAt = c.BlockedAt ?? DateTime.UtcNow,
                        BlockReason = c.BlockReason
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blocked users for user {UserId}", userId);
                throw;
            }
        }

        #endregion

        #region Notification Event Publishers

        /// <summary>
        /// Publish contact request notifications
        /// </summary>
        private async Task PublishContactRequestNotificationsAsync(Contact contact, User requester, User addressee)
        {
            try
            {
                // Push Notification
                await _eventPublisher.PublishAsync(new SendPushNotificationEvent
                {
                    EventId = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    UserId = addressee.Id.ToString(),
                    Target = NotificationTarget.User,
                    TargetId = addressee.Id.ToString(),
                    Method = "ContactRequestReceived",
                    Payload = new
                    {
                        contactId = contact.Id,
                        requesterId = requester.Id,
                        requesterUsername = requester.Username,
                        requesterDisplayName = requester.DisplayName,
                        requesterProfilePicture = requester.ProfilePictureUrl,
                        message = contact.Message
                    }
                });

                // Email Notification
                await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
                {
                    EventId = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    UserId = addressee.Id.ToString(),
                    To = addressee.Email,
                    Subject = "New Contact Request",
                    TemplateName = "ContactRequestReceived",
                    TemplateData = new Dictionary<string, string>
                {
                    { "AddresseeName", addressee.DisplayName ?? addressee.Username },
                    { "RequesterName", requester.DisplayName ?? requester.Username },
                    { "RequesterUsername", requester.Username },
                    { "RequestMessage", contact.Message ?? "No message" },
                    { "RequestDate", contact.RequestedAt.ToString("yyyy-MM-dd HH:mm:ss UTC") },
                    { "AcceptUrl", $"https://yourapp.com/contacts/requests/{contact.Id}/accept" },
                    { "RejectUrl", $"https://yourapp.com/contacts/requests/{contact.Id}/reject" }
                }
                });

                // Optional SMS Notification
                if (!string.IsNullOrEmpty(addressee.PhoneNumber))
                {
                    await _eventPublisher.PublishAsync(new SendSmsNotificationEvent
                    {
                        EventId = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow,
                        UserId = addressee.Id.ToString(),
                        PhoneNumber = addressee.PhoneNumber,
                        SmsBody = $"{requester.DisplayName ?? requester.Username} sent you a contact request."
                    });
                }

                _logger.LogInformation("Contact request notifications published for contact {ContactId}", contact.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish contact request notifications for contact {ContactId}", contact.Id);
                // Don't throw - notifications are not critical
            }
        }

        /// <summary>
        /// Publish contact accepted notifications
        /// </summary>
        private async Task PublishContactAcceptedNotificationsAsync(Contact contact, User requester, User addressee)
        {
            try
            {
                // Push Notification to Requester
                await _eventPublisher.PublishAsync(new SendPushNotificationEvent
                {
                    EventId = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    UserId = requester.Id.ToString(),
                    Target = NotificationTarget.User,
                    TargetId = requester.Id.ToString(),
                    Method = "ContactRequestAccepted",
                    Payload = new
                    {
                        contactId = contact.Id,
                        addresseeId = addressee.Id,
                        addresseeUsername = addressee.Username,
                        addresseeDisplayName = addressee.DisplayName,
                        addresseeProfilePicture = addressee.ProfilePictureUrl,
                        acceptedAt = contact.AcceptedAt
                    }
                });

                // Email Notification to Requester
                await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
                {
                    EventId = Guid.NewGuid(),
                    Timestamp = DateTime.UtcNow,
                    UserId = requester.Id.ToString(),
                    To = requester.Email,
                    Subject = "Contact Request Accepted",
                    TemplateName = "ContactRequestAccepted",
                    TemplateData = new Dictionary<string, string>
                {
                    { "RequesterName", requester.DisplayName ?? requester.Username },
                    { "AddresseeName", addressee.DisplayName ?? addressee.Username },
                    { "AddresseeUsername", addressee.Username },
                    { "AcceptedDate", contact.AcceptedAt?.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? DateTime.UtcNow.ToString() },
                    { "ChatUrl", $"https://yourapp.com/chat/{addressee.Id}" },
                    { "ProfileUrl", $"https://yourapp.com/profile/{addressee.Id}" }
                }
                });

                // Optional SMS Notification to Requester
                if (!string.IsNullOrEmpty(requester.PhoneNumber))
                {
                    await _eventPublisher.PublishAsync(new SendSmsNotificationEvent
                    {
                        EventId = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow,
                        UserId = requester.Id.ToString(),
                        PhoneNumber = requester.PhoneNumber,
                        SmsBody = $"{addressee.DisplayName ?? addressee.Username} accepted your contact request!"
                    });
                }

                _logger.LogInformation("Contact accepted notifications published for contact {ContactId}", contact.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish contact accepted notifications for contact {ContactId}", contact.Id);
                // Don't throw - notifications are not critical
            }
        }

        /// <summary>
        /// Publish contact rejected notifications
        /// </summary>
        private async Task PublishContactRejectedNotificationsAsync(Contact contact, User requester, User addressee, string reason)
        {
            try
            {
                // Only notify if there's a reason provided (optional behavior)
                if (!string.IsNullOrEmpty(reason))
                {
                    // Push Notification
                    await _eventPublisher.PublishAsync(new SendPushNotificationEvent
                    {
                        EventId = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow,
                        UserId = requester.Id.ToString(),
                        Target = NotificationTarget.User,
                        TargetId = requester.Id.ToString(),
                        Method = "ContactRequestRejected",
                        Payload = new
                        {
                            contactId = contact.Id,
                            message = "Your contact request was declined"
                        }
                    });

                    // Optional Email Notification
                    await _eventPublisher.PublishAsync(new SendEmailNotificationEvent
                    {
                        EventId = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow,
                        UserId = requester.Id.ToString(),
                        To = requester.Email,
                        Subject = "Contact Request Update",
                        TemplateName = "ContactRequestRejected",
                        TemplateData = new Dictionary<string, string>
                    {
                        { "RequesterName", requester.DisplayName ?? requester.Username },
                        { "RejectedDate", contact.RejectedAt?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd") }
                    }
                    });
                }

                _logger.LogInformation("Contact rejected notifications published for contact {ContactId}", contact.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish contact rejected notifications for contact {ContactId}", contact.Id);
                // Don't throw - notifications are not critical
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task ValidateContactRequestAsync(long requesterId, long addresseeId)
        {
            if (requesterId == addresseeId)
            {
                throw new InvalidOperationException("Cannot send a contact request to yourself");
            }

            // Check if users exist and are active
            var requester = await _userRepository.GetByIdAsync(requesterId);
            var addressee = await _userRepository.GetByIdAsync(addresseeId);

            if (requester == null || !requester.IsActive || requester.IsDeleted)
            {
                throw new NotFoundException("Requester not found or inactive");
            }

            if (addressee == null || !addressee.IsActive || addressee.IsDeleted)
            {
                throw new NotFoundException("Addressee not found or inactive");
            }

            // Check for existing relationship
            var existing = await _contactRepository.Table
                .FirstOrDefaultAsync(c =>
                    ((c.RequesterId == requesterId && c.AddresseeId == addresseeId) ||
                     (c.RequesterId == addresseeId && c.AddresseeId == requesterId)) &&
                    c.IsActive);

            if (existing != null)
            {
                switch (existing.Status)
                {
                    case ContactStatus.Pending:
                        throw new InvalidOperationException("A contact request is already pending");
                    case ContactStatus.Accepted:
                        throw new InvalidOperationException("You are already connected");
                    case ContactStatus.Blocked:
                        throw new InvalidOperationException("Cannot send request to this user");
                    case ContactStatus.Rejected:
                        // Allow resending after rejection (with cooldown)
                        if (existing.RejectedAt.HasValue &&
                            existing.RejectedAt.Value.AddDays(7) > DateTime.UtcNow)
                        {
                            throw new InvalidOperationException("Please wait before sending another request");
                        }
                        break;
                }
            }
        }

        private async Task<Dictionary<long, ContactRelationship>> GetExistingContactsMapAsync(long currentUserId, IEnumerable<long> userIds)
        {
            var contacts = await _contactRepository.Table
                .Where(c =>
                    ((c.RequesterId == currentUserId && userIds.Contains(c.AddresseeId)) ||
                     (c.AddresseeId == currentUserId && userIds.Contains(c.RequesterId))) &&
                    c.IsActive)
                .ToListAsync();

            var result = new Dictionary<long, ContactRelationship>();

            foreach (var userId in userIds)
            {
                var contact = contacts.FirstOrDefault(c =>
                    (c.RequesterId == currentUserId && c.AddresseeId == userId) ||
                    (c.AddresseeId == currentUserId && c.RequesterId == userId));

                if (contact != null)
                {
                    result[userId] = new ContactRelationship
                    {
                        Status = contact.Status,
                        IsSentByCurrentUser = contact.RequesterId == currentUserId
                    };
                }
            }

            return result;
        }

        private async Task InvalidateContactsCacheAsync(params long[] userIds)
        {
            foreach (var userId in userIds)
            {
                var contactsCacheKey = new CacheKey(string.Format(CONTACTS_CACHE_KEY, userId));
                var requestsCacheKey = new CacheKey(string.Format(PENDING_REQUESTS_CACHE_KEY, userId));
                var blockedCacheKey = new CacheKey(string.Format(BLOCKED_USERS_CACHE_KEY, userId));

                await _cache.RemoveAsync(contactsCacheKey);
                await _cache.RemoveAsync(requestsCacheKey);
                await _cache.RemoveAsync(blockedCacheKey);
            }
        }

        private UserSearchDto MapToUserSearchDto(User user, Dictionary<long, ContactRelationship> contactsMap)
        {
            var relationship = contactsMap.GetValueOrDefault(user.Id);

            return new UserSearchDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Bio = user.Bio,
                IsOnline = user.IsOnline,
                LastSeen = user.LastSeen,
                ContactStatus = relationship?.Status,
                IsSentByCurrentUser = relationship?.IsSentByCurrentUser ?? false
            };
        }

        private ContactDto MapToContactDto(Contact contact, User requester, User addressee, long? currentUserId = null)
        {
            var otherUser = currentUserId.HasValue
                ? (contact.RequesterId == currentUserId ? addressee : requester)
                : (requester.Id == contact.RequesterId ? addressee : requester);

            return new ContactDto
            {
                Id = contact.Id,
                UserId = otherUser.Id,
                Username = otherUser.Username,
                DisplayName = otherUser.DisplayName,
                Email = otherUser.Email,
                ProfilePictureUrl = otherUser.ProfilePictureUrl,
                Bio = otherUser.Bio,
                Status = contact.Status,
                IsOnline = otherUser.IsOnline,
                LastSeen = otherUser.LastSeen,
                ConnectedAt = contact.AcceptedAt ?? contact.RequestedAt,
                IsFavorite = contact.IsFavorite,
                IsBlocked = contact.Status == ContactStatus.Blocked
            };
        }

        private ContactRequestDto MapToContactRequestDto(Contact contact, Dictionary<long, User> users, long currentUserId)
        {
            var isReceived = contact.AddresseeId == currentUserId;
            var otherUserId = isReceived ? contact.RequesterId : contact.AddresseeId;
            var otherUser = users.GetValueOrDefault(otherUserId);

            return new ContactRequestDto
            {
                Id = contact.Id,
                UserId = otherUserId,
                Username = otherUser?.Username,
                DisplayName = otherUser?.DisplayName,
                ProfilePictureUrl = otherUser?.ProfilePictureUrl,
                Message = contact.Message,
                RequestedAt = contact.RequestedAt,
                IsReceived = isReceived,
                Status = contact.Status
            };
        }

        #endregion
    }
}