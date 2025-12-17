using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VideoConferencingApp.Api.DTOs;
using VideoConferencingApp.API.Controllers.Base;
using VideoConferencingApp.Application.DTOs.Chat;
using VideoConferencingApp.Application.Services.MessagingServices;
using VideoConferencingApp.Infrastructure.Services.AuthServices;

namespace VideoConferencingApp.Api.Controllers
{
    public class ChatHistoryController : BaseController
    {
        private readonly IChatService _chatService;
        private readonly IGroupService _groupService;
        private readonly ILogger<ChatHistoryController> _logger;

        public ChatHistoryController(
            IChatService chatService,
            IGroupService groupService,
            ICurrentUserService currentUserService,
            IHttpContextService httpContextService,
            IResponseHeaderService responseHeaderService,
            ILogger<ChatHistoryController> logger)
            : base(logger, currentUserService, httpContextService, responseHeaderService)
        {
            _chatService = chatService;
            _groupService = groupService;
            _logger = logger;
        }

        /// <summary>
        /// Get direct chat history with another user
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IList<ChatMessageDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IList<ChatMessageDto>>>> GetDirectChatHistory(
            string userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IList<ChatMessageDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var chatHistory = await _chatService.GetChatHistoryAsync(
                    CurrentUserId.Value.ToString(),
                    new ChatHistoryDto
                    {
                        UserId = userId,
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    });

                _logger.LogInformation(
                    "Direct chat history retrieved - CurrentUser: {CurrentUserId}, OtherUser: {UserId}, Count: {Count}",
                    CurrentUserId, userId, chatHistory.Count);

                return Success(chatHistory, $"Retrieved {chatHistory.Count} messages");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting direct chat history with user {UserId}", userId);
                return Failure<IList<ChatMessageDto>>(
                    null,
                    "An error occurred while retrieving chat history.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get group chat history
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IList<ChatMessageDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<IList<ChatMessageDto>>>> GetGroupChatHistory(
            string groupId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IList<ChatMessageDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                // Verify user is member of group
                var group = await _groupService.GetGroupAsync(groupId);
                if (group == null)
                {
                    return Failure<IList<ChatMessageDto>>(
                        null,
                        "Group not found.",
                        StatusCodes.Status404NotFound);
                }

                var isMember = group.Members.Any(m => m.UserId == CurrentUserId.Value.ToString());
                if (!isMember)
                {
                    return Failure<IList<ChatMessageDto>>(
                        null,
                        "You are not a member of this group.",
                        StatusCodes.Status403Forbidden);
                }

                var chatHistory = await _chatService.GetChatHistoryAsync(
                    CurrentUserId.Value.ToString(),
                    new ChatHistoryDto
                    {
                        GroupId = groupId,
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    });

                _logger.LogInformation(
                    "Group chat history retrieved - GroupId: {GroupId}, UserId: {UserId}, Count: {Count}",
                    groupId, CurrentUserId, chatHistory.Count);

                return Success(chatHistory, $"Retrieved {chatHistory.Count} messages");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group chat history for group {GroupId}", groupId);
                return Failure<IList<ChatMessageDto>>(
                    null,
                    "An error occurred while retrieving group chat history.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get recent conversations (last message with each contact/group)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IList<ConversationDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IList<ConversationDto>>>> GetConversations(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IList<ConversationDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var conversations = await _chatService.GetConversationsAsync(
                    CurrentUserId.Value.ToString(),
                    pageNumber,
                    pageSize);

                return Success(conversations, $"Retrieved {conversations.Count} conversations");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations for user {UserId}", CurrentUserId);
                return Failure<IList<ConversationDto>>(
                    null,
                    "An error occurred while retrieving conversations.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Search messages
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IList<ChatMessageDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IList<ChatMessageDto>>>> SearchMessages(
            [FromQuery] string query,
            [FromQuery] string? userId = null,
            [FromQuery] string? groupId = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IList<ChatMessageDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                if (string.IsNullOrWhiteSpace(query))
                {
                    return Failure<IList<ChatMessageDto>>(
                        null,
                        "Search query is required.",
                        StatusCodes.Status400BadRequest);
                }

                var messages = await _chatService.SearchMessagesAsync(
                    CurrentUserId.Value.ToString(),
                    query,
                    userId,
                    groupId,
                    pageNumber,
                    pageSize);

                return Success(messages, $"Found {messages.Count} messages");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages for user {UserId}", CurrentUserId);
                return Failure<IList<ChatMessageDto>>(
                    null,
                    "An error occurred while searching messages.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get unread messages count
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<UnreadCountDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<UnreadCountDto>>> GetUnreadCount()
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<UnreadCountDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var unreadCount = await _chatService.GetUnreadCountAsync(CurrentUserId.Value.ToString());

                return Success(unreadCount, "Unread count retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", CurrentUserId);
                return Failure<UnreadCountDto>(
                    null,
                    "An error occurred while retrieving unread count.",
                    StatusCodes.Status500InternalServerError);
            }
        }
    }
}

