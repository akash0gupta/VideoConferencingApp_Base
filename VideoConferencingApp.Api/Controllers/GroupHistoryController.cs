using Microsoft.AspNetCore.Mvc;
using VideoConferencingApp.Api.DTOs;
using VideoConferencingApp.API.Controllers.Base;
using VideoConferencingApp.Application.DTOs.Chat;
using VideoConferencingApp.Application.DTOs.Group;
using VideoConferencingApp.Application.Services.MessagingServices;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Infrastructure.Services.AuthServices;

namespace VideoConferencingApp.Api.Controllers
{
    public class GroupHistoryController : BaseController
    {
        private readonly IGroupService _groupService;
        private readonly IChatService _chatService;
        private readonly ILogger<GroupHistoryController> _logger;

        public GroupHistoryController(
            IGroupService groupService,
            IChatService chatService,
            ICurrentUserService currentUserService,
            IHttpContextService httpContextService,
            IResponseHeaderService responseHeaderService,
            ILogger<GroupHistoryController> logger)
            : base(logger, currentUserService, httpContextService, responseHeaderService)
        {
            _groupService = groupService;
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// Get group activity history
        /// </summary>
        [HttpGet("{groupId}/activity")]
        [ProducesResponseType(typeof(ApiResponse<IList<GroupActivityDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IList<GroupActivityDto>>>> GetGroupActivity(
            string groupId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IList<GroupActivityDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var activity = await _groupService.GetGroupActivityAsync(groupId, pageNumber, pageSize);

                return Success(activity, $"Retrieved {activity.Count} activity items");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group activity for group {GroupId}", groupId);
                return Failure<IList<GroupActivityDto>>(
                    null,
                    "An error occurred while retrieving group activity.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get group member join/leave history
        /// </summary>
        [HttpGet("{groupId}/member-history")]
        [ProducesResponseType(typeof(ApiResponse<IList<GroupMemberHistoryDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IList<GroupMemberHistoryDto>>>> GetMemberHistory(
            string groupId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IList<GroupMemberHistoryDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var history = await _groupService.GetMemberHistoryAsync(groupId, pageNumber, pageSize);

                return Success(history, $"Retrieved {history.Count} member history items");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member history for group {GroupId}", groupId);
                return Failure<IList<GroupMemberHistoryDto>>(
                    null,
                    "An error occurred while retrieving member history.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get group media/files history
        /// </summary>
        [HttpGet("{groupId}/media")]
        [ProducesResponseType(typeof(ApiResponse<IList<GroupMediaDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IList<GroupMediaDto>>>> GetGroupMedia(
            string groupId,
            [FromQuery] MediaType? mediaType = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IList<GroupMediaDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var media = await _chatService.GetGroupMediaAsync(
                    groupId,
                    CurrentUserId.Value.ToString(),
                    mediaType,
                    pageNumber,
                    pageSize);

                return Success(media, $"Retrieved {media.Count} media items");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group media for group {GroupId}", groupId);
                return Failure<IList<GroupMediaDto>>(
                    null,
                    "An error occurred while retrieving group media.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get shared files in group
        /// </summary>
        [HttpGet("{groupId}/files")]
        [ProducesResponseType(typeof(ApiResponse<IList<SharedFileDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IList<SharedFileDto>>>> GetSharedFiles(
            string groupId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IList<SharedFileDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var files = await _chatService.GetGroupSharedFilesAsync(
                    groupId,
                    CurrentUserId.Value.ToString(),
                    pageNumber,
                    pageSize);

                return Success(files, $"Retrieved {files.Count} shared files");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shared files for group {GroupId}", groupId);
                return Failure<IList<SharedFileDto>>(
                    null,
                    "An error occurred while retrieving shared files.",
                    StatusCodes.Status500InternalServerError);
            }
        }
    }
}

