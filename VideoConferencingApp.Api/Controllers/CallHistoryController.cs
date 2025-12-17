
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using VideoConferencingApp.Api.Controllers;
using VideoConferencingApp.Api.DTOs;
using VideoConferencingApp.API.Controllers.Base;
using VideoConferencingApp.Application.DTOs.Authentication;
using VideoConferencingApp.Application.DTOs.Call;
using VideoConferencingApp.Application.Services.MessagingServices;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Interfaces;
using VideoConferencingApp.Infrastructure.Services.AuthServices;

namespace VideoConferencingApp.Api.Controllers
{
    public class CallHistoryController : BaseController
    {
        private readonly ICallService _callService;
        private readonly ILogger<CallHistoryController> _logger;

        public CallHistoryController(
            ICallService callService,
            ICurrentUserService currentUserService,
            IHttpContextService httpContextService,
            IResponseHeaderService responseHeaderService,
            ILogger<CallHistoryController> logger)
            : base(logger, currentUserService, httpContextService, responseHeaderService)
        {
            _callService = callService;
            _logger = logger;
        }

        /// <summary>
        /// Get call history
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IPagedList<CallHistoryDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<IPagedList<CallHistoryDto>>>> GetCallHistory(
            [FromQuery] CallsType? callType = null,
            [FromQuery] CallStatus? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IPagedList<CallHistoryDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var history = await _callService.GetCallHistoryAsync(
                    CurrentUserId.Value.ToString(),
                    callType,
                    status,
                    pageNumber,
                    pageSize);

                // Set pagination headers
                if (history != null)
                {
                    _responseHeaderService.SetPaginationHeaders(new PaginationMetadata
                    {
                        CurrentPage = pageNumber,
                        PageSize = history.PageSize,
                        TotalPages = history.TotalPages,
                        TotalCount = history.TotalCount,
                        HasNext = history.HasNextPage,
                        HasPrevious = history.HasPreviousPage
                    });
                }

                _logger.LogInformation(
                    "Call history retrieved - UserId: {UserId}, Count: {Count}",
                    CurrentUserId, history?.Count ?? 0);

                return Success(history, $"Retrieved {history?.Count ?? 0} calls");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting call history for user {UserId}", CurrentUserId);
                return Failure<IPagedList<CallHistoryDto>>(
                    null,
                    "An error occurred while retrieving call history.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get call statistics
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(ApiResponse<CallStatisticsDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<CallStatisticsDto>>> GetCallStatistics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<CallStatisticsDto>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var statistics = await _callService.GetCallStatisticsAsync(
                    CurrentUserId.Value.ToString(),
                    fromDate ?? DateTime.UtcNow.AddMonths(-1),
                    toDate ?? DateTime.UtcNow);

                return Success(statistics, "Statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting call statistics for user {UserId}", CurrentUserId);
                return Failure<CallStatisticsDto>(
                    null,
                    "An error occurred while retrieving call statistics.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get missed calls
        /// </summary>
        [HttpGet("missed")]
        [ProducesResponseType(typeof(ApiResponse<IList<CallHistoryDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IList<CallHistoryDto>>>> GetMissedCalls(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (!IsAuthenticated || !CurrentUserId.HasValue)
                {
                    return Failure<IList<CallHistoryDto>>(
                        null,
                        "User is not authorized.",
                        StatusCodes.Status401Unauthorized);
                }

                var missedCalls = await _callService.GetMissedCallsAsync(
                    CurrentUserId.Value.ToString(),
                    pageNumber,
                    pageSize);

                return Success(missedCalls, $"Retrieved {missedCalls.Count} missed calls");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting missed calls for user {UserId}", CurrentUserId);
                return Failure<IList<CallHistoryDto>>(
                    null,
                    "An error occurred while retrieving missed calls.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Delete call from history
        /// </summary>
        [HttpDelete("{callId}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCallHistory(string callId)
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

                var deleted = await _callService.DeleteCallHistoryAsync(callId, CurrentUserId.Value.ToString());

                if (deleted)
                {
                    return Success(true, "Call deleted from history");
                }

                return Failure<bool>(false, "Failed to delete call", StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting call history {CallId}", callId);
                return Failure<bool>(
                    false,
                    "An error occurred while deleting call history.",
                    StatusCodes.Status500InternalServerError);
            }
        }
    }
}

