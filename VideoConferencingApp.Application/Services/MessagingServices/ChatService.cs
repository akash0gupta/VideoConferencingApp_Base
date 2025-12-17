using LinqToDB;
using Mapster;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Application.DTOs.Chat;
using VideoConferencingApp.Application.Services.MessagingServices;
using VideoConferencingApp.Domain.Entities.ChatEntities;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Interfaces;

namespace VideoConferencingApp.Application.Services.MessagingServices
{
    public class ChatService : IChatService
    {
        private readonly IRepository<Message> _messageRepository;
        private readonly IRepository<GroupMember> _groupMemberRepository;
        private readonly IRepository<ChatGroup> _groupRepository;
        private readonly IRepository<MessageReceipt> _mssageReceiptRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ChatService> _logger;

     public ChatService(
     IRepository<MessageReceipt> mssageReceiptRepository,
     IRepository<GroupMember> groupMemberRepository,
     IRepository<Message> messageRepository,
     IRepository<User> userRepository,
     IRepository<ChatGroup> groupRepository,
     IUnitOfWork unitOfWork,
     ILogger<ChatService> logger)
        {
            _mssageReceiptRepository = mssageReceiptRepository;
            _groupMemberRepository = groupMemberRepository;
            _messageRepository = messageRepository;
            _groupRepository = groupRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ChatMessageDto> SendMessageAsync(string senderId, SendMessageDto dto)
        {
            try
            {
                // Get sender info
                var sender = await _userRepository.GetByIdAsync(long.Parse(senderId));
                if (sender == null)
                    throw new InvalidOperationException("Sender not found");

                // Create message entity
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = dto.ReceiverId,
                    GroupId = dto.GroupId,
                    Content = dto.Content,
                    Type = dto.Type,
                    Status = MessageStatus.Sent,
                    SentAt = DateTime.UtcNow,
                    ReplyToMessageId = dto.ReplyToMessageId,
                    Metadata = dto.Metadata != null ? System.Text.Json.JsonSerializer.Serialize(dto.Metadata) : null
                };

                await _messageRepository.InsertAsync(message);
                await _unitOfWork.SaveChangesAsync();

                // Map to DTO
                var messageDto = message.Adapt<ChatMessageDto>();
                messageDto.SenderName = sender.DisplayName ?? sender.Username;
                messageDto.SenderAvatar = sender.ProfilePictureUrl;

                _logger.LogInformation("Message {MessageId} sent from {SenderId} to {ReceiverId}/{GroupId}",
                    message.Id, senderId, dto.ReceiverId ?? "N/A", dto.GroupId ?? "N/A");

                return messageDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message from {SenderId}", senderId);
                throw;
            }
        }

        public async Task<IList<ChatMessageDto>> GetChatHistoryAsync(string userId, ChatHistoryDto dto)
        {
            try
            {
                IList<Message> messages;

                if (!string.IsNullOrEmpty(dto.UserId))
                {
                    // Direct chat
                    messages = await _messageRepository.Table
               .Where(m =>
                   ((m.SenderId == userId && m.ReceiverId == dto.UserId) ||
                    (m.SenderId == dto.UserId && m.ReceiverId == userId)) &&
                   !m.IsDeleted)
               .OrderByDescending(m => m.SentAt)
               .Skip((dto.PageNumber - 1) * dto.PageSize)
               .Take(dto.PageSize).ToListAsync();
                }
                else if (!string.IsNullOrEmpty(dto.GroupId))
                {
                    // Group chat
                    messages = await _messageRepository.Table
                    .Where(m => m.GroupId == dto.GroupId && !m.IsDeleted)
                .OrderByDescending(m => m.SentAt)
                .Skip((dto.PageNumber - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();

                }
                else
                {
                    throw new ArgumentException("Either UserId or GroupId must be provided");
                }

                return messages.Adapt<IList<ChatMessageDto>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat history for user {UserId}", userId);
                throw;
            }
        }

        public async Task MarkMessagesAsDeliveredAsync(string messageId, string userId)
        {
            try
            {
                await MarkAsDeliveredAsync(messageId, userId);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogDebug("Message {MessageId} marked as delivered for user {UserId}", messageId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message as delivered");
                throw;
            }
        }

        public async Task MarkMessagesAsReadAsync(List<string> messageIds, string userId)
        {
            try
            {
                await MarkAsReadAsync(messageIds, userId);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogDebug("Messages marked as read for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read");
                throw;
            }
        }

        public async Task<IList<ChatMessageDto>> GetUndeliveredMessagesAsync(string userId)
        {
            try
            {
                var messages = await _messageRepository.Table
                 .Where(m =>
                    m.ReceiverId == userId &&
                    m.Status == MessageStatus.Sent &&
                    !m.IsDeleted)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
                return messages.Adapt<IList<ChatMessageDto>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting undelivered messages for user {UserId}", userId);
                throw;
            }
        }

        public async Task MarkAsDeliveredAsync(string messageId, string userId)
        {
            var message = await _messageRepository.Table
                .FirstOrDefaultAsync(m => m.Id.ToString() == messageId);

            if (message != null && message.Status == MessageStatus.Sent)
            {
                message.Status = MessageStatus.Delivered;
                message.DeliveredAt = DateTime.UtcNow;

                // Add receipt
                var receipt = new MessageReceipt
                {
                    MessageId = messageId,
                    UserId = userId,
                    Status = MessageStatus.Delivered,
                    Timestamp = DateTime.UtcNow
                };

                await _mssageReceiptRepository.InsertAsync(receipt);
            }
        }

        public async Task MarkAsReadAsync(List<string> messageIds, string userId)
        {
            var messages = await _messageRepository.Table
                .Where(m => messageIds.Contains(m.Id.ToString()))
                .ToListAsync();

            foreach (var message in messages)
            {
                if (message.Status != MessageStatus.Read)
                {
                    message.Status = MessageStatus.Read;
                    message.ReadAt = DateTime.UtcNow;

                    // Add receipt
                    var receipt = new MessageReceipt
                    {
                        MessageId = message.Id.ToString(),
                        UserId = userId,
                        Status = MessageStatus.Read,
                        Timestamp = DateTime.UtcNow
                    };

                    await _mssageReceiptRepository.InsertAsync(receipt);
                }
            }
        }

        public async Task<IList<ConversationDto>> GetConversationsAsync(
                   string userId,
                   int pageNumber,
                   int pageSize)
        {
            try
            {
                // All direct conversations (partners)
                var directPartners = await _messageRepository.Table
                    .Where(m => (m.SenderId == userId || m.ReceiverId == userId) &&
                                m.GroupId == null &&
                                !m.IsDeleted)
                    .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                    .Distinct()
                    .ToListAsync();

                // All groups user is in
                var userGroupIds = await _groupMemberRepository.Table
                    .Where(gm => gm.UserId == userId && !gm.IsDeleted)
                    .Select(gm => gm.GroupId)
                    .Distinct()
                    .ToListAsync();

                var lastDirectMessages = (await _messageRepository.Table
    .Where(m =>
        (directPartners.Contains(m.SenderId) || directPartners.Contains(m.ReceiverId)) &&
        !m.IsDeleted &&
        m.GroupId == null)
    .ToListAsync())
    .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
    .Select(g => g.OrderByDescending(m => m.SentAt).First())
    .ToList();

                var lastGroupMessages = (await _messageRepository.Table
    .Where(m => userGroupIds.Contains(m.GroupId) && !m.IsDeleted)
    .ToListAsync())
    .GroupBy(m => m.GroupId)
    .Select(g => g.OrderByDescending(m => m.SentAt).First())
    .ToList();


                var unreadCounts = await _messageRepository.Table
    .Where(m =>
        !m.IsDeleted &&
        m.Status != MessageStatus.Read &&
        (
            // unread from direct chats
            (m.ReceiverId == userId && directPartners.Contains(m.SenderId)) ||

            // unread in groups
            (m.GroupId != null && userGroupIds.Contains(m.GroupId))
        ))
    .GroupBy(m => new {
        ConversationKey = m.GroupId ?? m.SenderId,  // group or friend
        m.GroupId
    })
    .Select(g => new {
        Id = g.Key.ConversationKey,
        g.Key.GroupId,
        Count = g.Count()
    })
    .ToListAsync();

                var conversations = new List<ConversationDto>();

                var allLastMessages = lastDirectMessages.Concat(lastGroupMessages)
                    .OrderByDescending(m => m.SentAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                foreach (var message in allLastMessages)
                {
                    var unread = unreadCounts.FirstOrDefault(x =>
                        (message.GroupId != null && x.GroupId == message.GroupId) ||
                        (message.GroupId == null && x.Id ==
                            (message.SenderId == userId ? message.ReceiverId : message.SenderId)));

                    if (message.GroupId != null)
                    {
                        // group
                        var group = await _groupRepository.GetByIdAsync(long.Parse(message.GroupId));
                        if (group == null) continue;

                        conversations.Add(new ConversationDto
                        {
                            Id = message.GroupId,
                            Type = ConversationType.Group,
                            GroupId = message.GroupId,
                            Name = group.GroupName,
                            Avatar = group.AvatarUrl,
                            LastMessage = message.Adapt<ChatMessageDto>(),
                            UnreadCount = unread?.Count ?? 0,
                            LastActivityAt = message.SentAt
                        });
                    }
                    else
                    {
                        // direct chat
                        var otherId = message.SenderId == userId ? message.ReceiverId : message.SenderId;
                        var otherUser = await _userRepository.GetByIdAsync(long.Parse(otherId!));
                        if (otherUser == null) continue;

                        conversations.Add(new ConversationDto
                        {
                            Id = otherId,
                            Type = ConversationType.Direct,
                            UserId = otherId,
                            Name = otherUser.DisplayName ?? otherUser.Username,
                            Avatar = otherUser.ProfilePictureUrl,
                            LastMessage = message.Adapt<ChatMessageDto>(),
                            UnreadCount = unread?.Count ?? 0,
                            LastActivityAt = message.SentAt
                        });
                    }
                }
                return conversations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IList<ChatMessageDto>> SearchMessagesAsync(
                    string userId,
                    string query,
                    string? otherUserId,
                    string? groupId,
                    int pageNumber,
                    int pageSize)
        {
            try
            {
                var queryLower = query.ToLower();

                var messagesQuery = _messageRepository.Table
                    .Where(m => !m.IsDeleted && m.Content.ToLower().Contains(queryLower));

                // Filter by conversation type
                if (!string.IsNullOrEmpty(otherUserId))
                {
                    messagesQuery = messagesQuery.Where(m =>
                        ((m.SenderId == userId && m.ReceiverId == otherUserId) ||
                         (m.SenderId == otherUserId && m.ReceiverId == userId)) &&
                        m.GroupId == null);
                }
                else if (!string.IsNullOrEmpty(groupId))
                {
                    messagesQuery = messagesQuery.Where(m => m.GroupId == groupId);
                }
                else
                {
                    // Search in all user's conversations
                    var userGroupIds = await _groupMemberRepository.Table
                        .Where(gm => gm.UserId == userId && !gm.IsDeleted)
                        .Select(gm => gm.GroupId)
                        .ToListAsync();

                    messagesQuery = messagesQuery.Where(m =>
                        m.SenderId == userId ||
                        m.ReceiverId == userId ||
                        userGroupIds.Contains(m.GroupId));
                }

                var messages = await messagesQuery
                    .OrderByDescending(m => m.SentAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return messages.Adapt<IList<ChatMessageDto>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages for user {UserId}", userId);
                throw;
            }
        }

        public async Task<UnreadCountDto> GetUnreadCountAsync(string userId)
        {
            try
            {
                var unreadMessages = await _messageRepository.Table
                    .Where(m =>
                        m.ReceiverId == userId &&
                        m.Status != MessageStatus.Read &&
                        !m.IsDeleted)
                    .ToListAsync();

                var unreadByConversation = new Dictionary<string, int>();

                // Group by direct chats
                var directChats = unreadMessages
                    .Where(m => m.GroupId == null)
                    .GroupBy(m => m.SenderId)
                    .ToDictionary(g => $"user_{g.Key}", g => g.Count());

                // Group by group chats
                var groupChats = unreadMessages
                    .Where(m => m.GroupId != null)
                    .GroupBy(m => m.GroupId!)
                    .ToDictionary(g => $"group_{g.Key}", g => g.Count());

                foreach (var chat in directChats)
                    unreadByConversation.Add(chat.Key, chat.Value);

                foreach (var chat in groupChats)
                    unreadByConversation.Add(chat.Key, chat.Value);

                return new UnreadCountDto
                {
                    TotalUnread = unreadMessages.Count,
                    UnreadByConversation = unreadByConversation
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                throw;
            }
        }
        public async Task<IList<GroupMediaDto>> GetGroupMediaAsync(
             string groupId,
             string userId,
             MediaType? mediaType,
             int pageNumber,
             int pageSize)
        {
            try
            {
                // Verify user is member of group
                var isMember = await _groupMemberRepository.Table
                    .AnyAsync(m => m.GroupId == groupId && m.UserId == userId && !m.IsDeleted);

                if (!isMember)
                    throw new UnauthorizedAccessException("User is not a member of this group");

                var query = _messageRepository.Table
                    .Where(m => m.GroupId == groupId && !m.IsDeleted);

                // Filter by media type
                if (mediaType.HasValue)
                {
                    var messageType = mediaType.Value switch
                    {
                        MediaType.Image => MessageType.Image,
                        MediaType.Video => MessageType.Video,
                        MediaType.Audio => MessageType.Audio,
                        MediaType.Document => MessageType.File,
                        _ => MessageType.File
                    };

                    query = query.Where(m => m.Type == messageType);
                }
                else
                {
                    // Only media types (not text)
                    query = query.Where(m =>
                        m.Type == MessageType.Image ||
                        m.Type == MessageType.Video ||
                        m.Type == MessageType.Audio ||
                        m.Type == MessageType.File);
                }

                var messages = await query
                    .OrderByDescending(m => m.SentAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var mediaDtos = new List<GroupMediaDto>();

                foreach (var message in messages)
                {
                    var sender = await _userRepository.GetByIdAsync(long.Parse(message.SenderId));

                    var metadata = !string.IsNullOrEmpty(message.Metadata)
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(message.Metadata)
                        : null;

                    mediaDtos.Add(new GroupMediaDto
                    {
                        MessageId = message.Id.ToString(),
                        SenderId = message.SenderId,
                        SenderName = sender?.DisplayName ?? sender?.Username ?? "Unknown",
                        Type = message.Type switch
                        {
                            MessageType.Image => MediaType.Image,
                            MessageType.Video => MediaType.Video,
                            MessageType.Audio => MediaType.Audio,
                            MessageType.File => MediaType.Document,
                            _ => MediaType.Document
                        },
                        Url = metadata?.GetValueOrDefault("url")?.ToString() ?? message.Content,
                        ThumbnailUrl = metadata?.GetValueOrDefault("thumbnailUrl")?.ToString(),
                        FileName = metadata?.GetValueOrDefault("fileName")?.ToString(),
                        FileSize = metadata?.GetValueOrDefault("fileSize") != null
                            ? long.Parse(metadata["fileSize"].ToString()!)
                            : null,
                        SentAt = message.SentAt
                    });
                }

                return mediaDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group media for group {GroupId}", groupId);
                throw;
            }
        }


        public async Task<IList<SharedFileDto>> GetGroupSharedFilesAsync(
            string groupId,
            string userId,
            int pageNumber,
            int pageSize)
        {
            try
            {
                // Verify user is member of group
                var isMember = await _groupMemberRepository.Table
                    .AnyAsync(m => m.GroupId == groupId && m.UserId == userId && !m.IsDeleted);

                if (!isMember)
                    throw new UnauthorizedAccessException("User is not a member of this group");

                var messages = await _messageRepository.Table
                    .Where(m =>
                        m.GroupId == groupId &&
                        m.Type == MessageType.File &&
                        !m.IsDeleted)
                    .OrderByDescending(m => m.SentAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var fileDtos = new List<SharedFileDto>();

                foreach (var message in messages)
                {
                    var sender = await _userRepository.GetByIdAsync(long.Parse(message.SenderId));

                    var metadata = !string.IsNullOrEmpty(message.Metadata)
                        ? JsonSerializer.Deserialize<Dictionary<string, object>>(message.Metadata)
                        : null;

                    fileDtos.Add(new SharedFileDto
                    {
                        FileId = metadata?.GetValueOrDefault("fileId")?.ToString() ?? message.Id.ToString(),
                        MessageId = message.Id.ToString(),
                        SenderId = message.SenderId,
                        SenderName = sender?.DisplayName ?? sender?.Username ?? "Unknown",
                        SenderAvatar = sender?.ProfilePictureUrl,
                        FileName = metadata?.GetValueOrDefault("fileName")?.ToString() ?? "Unknown File",
                        FileSize = metadata?.GetValueOrDefault("fileSize") != null
                            ? long.Parse(metadata["fileSize"].ToString()!)
                            : 0,
                        ContentType = metadata?.GetValueOrDefault("contentType")?.ToString() ?? "application/octet-stream",
                        FileUrl = metadata?.GetValueOrDefault("url")?.ToString() ?? message.Content,
                        ThumbnailUrl = metadata?.GetValueOrDefault("thumbnailUrl")?.ToString(),
                        SharedAt = message.SentAt
                    });
                }

                return fileDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shared files for group {GroupId}", groupId);
                throw;
            }
        }
    }
}