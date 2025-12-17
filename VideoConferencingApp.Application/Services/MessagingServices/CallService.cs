using LinqToDB;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Application.DTOs.Call;
using VideoConferencingApp.Application.DTOs.Common;
using VideoConferencingApp.Application.Services.MessagingServices;
using VideoConferencingApp.Domain.Entities.CallEntities;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Interfaces;

namespace VideoConferencingApp.Application.Services.MessagingServices
{
    public class CallService : ICallService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CallService> _logger;
        private readonly IRepository<Call> _callRepository;

        public CallService(
            IRepository<Call> callRepository,
            IRepository<User> userRepository,
            IUnitOfWork unitOfWork,
            ILogger<CallService> logger)
        {
            _callRepository = callRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CallInitiatedDto> InitiateCallAsync(string callerId, InitiateCallDto dto)
        {
            try
            {
                // Check if user already in a call
                var existingCall = await _callRepository.Table
                .FirstOrDefaultAsync(c =>
                    (c.CallerId == callerId || c.ReceiverId == callerId) &&
                    (c.Status == CallStatus.Ringing || c.Status == CallStatus.Accepted) &&
                    !c.IsDeleted);

                if (existingCall != null)
                    throw new InvalidOperationException("User is already in a call");

               // Check if receiver is in a call
                if (!string.IsNullOrEmpty(dto.ReceiverId))
                {
                    var receiverCall = await _callRepository.Table
                .FirstOrDefaultAsync(c =>
                    (c.CallerId == dto.ReceiverId || c.ReceiverId == dto.ReceiverId) &&
                    (c.Status == CallStatus.Ringing || c.Status == CallStatus.Accepted) &&
                    !c.IsDeleted);
                    if (receiverCall != null)
                        throw new InvalidOperationException("Receiver is busy in another call");
                }

                var caller = await _userRepository.GetByIdAsync(long.Parse(callerId)) ?? throw new InvalidOperationException("Caller not found");

                // Create call
                var call = new Call
                {
                    CallerId = callerId,
                    ReceiverId = dto.ReceiverId,
                    GroupId = dto.GroupId,
                    Type = dto.CallType,
                    Status = CallStatus.Ringing,
                    InitiatedAt = DateTime.UtcNow
                };

                await _callRepository.InsertAsync(call);
                await _unitOfWork.SaveChangesAsync();

                var result = call.Adapt<CallInitiatedDto>();
                result.CallerName = caller.DisplayName ?? caller.Username;
                result.CallerAvatar = caller.ProfilePictureUrl;

                _logger.LogInformation("Call {CallId} initiated by {CallerId} to {ReceiverId}/{GroupId}",
                    call.Id, callerId, dto.ReceiverId ?? "N/A", dto.GroupId ?? "N/A");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating call");
                throw;
            }
        }

        public async Task<bool> AcceptCallAsync(string callId, string userId)
        {
            try
            {
                var call = await _callRepository.GetByIdAsync(long.Parse(callId)) ?? throw new InvalidOperationException("Call not found");
                if (call.Status != CallStatus.Ringing)
                    throw new InvalidOperationException($"Call is not in ringing state. Current status: {call.Status}");

                call.Status = CallStatus.Accepted;
                call.ConnectedAt = DateTime.UtcNow;

                await _callRepository.UpdateAsync(call);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Call {CallId} accepted by user {UserId}", callId, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting call {CallId}", callId);
                throw;
            }
        }

        public async Task<bool> RejectCallAsync(string callId, string userId, string? reason)
        {
            try
            {
                var call = await _callRepository.GetByIdAsync(long.Parse(callId)) ?? throw new InvalidOperationException("Call not found");
                call.Status = CallStatus.Rejected;
                call.EndedAt = DateTime.UtcNow;
                call.EndReason = reason ?? "Rejected by user";

                await _callRepository.UpdateAsync(call);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Call {CallId} rejected by user {UserId}", callId, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting call {CallId}", callId);
                throw;
            }
        }

        public async Task<CallEndedDto> EndCallAsync(string callId, string userId)
        {
            try
            {
                var call = await _callRepository.GetByIdAsync(long.Parse(callId));
                if (call == null)
                    throw new InvalidOperationException("Call not found");

                var endedAt = DateTime.UtcNow;
                call.Status = CallStatus.Ended;
                call.EndedAt = endedAt;

                if (call.ConnectedAt.HasValue)
                {
                    call.DurationSeconds = (int)(endedAt - call.ConnectedAt.Value).TotalSeconds;
                }

                await _callRepository.UpdateAsync(call);
                await _unitOfWork.SaveChangesAsync();

                var result = new CallEndedDto
                {
                    CallId = callId,
                    UserId = userId,
                    EndedAt = endedAt,
                    DurationSeconds = call.DurationSeconds
                };

                _logger.LogInformation("Call {CallId} ended by user {UserId} - Duration: {Duration}s",
                    callId, userId, call.DurationSeconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending call {CallId}", callId);
                throw;
            }
        }

        public async Task<bool> ToggleMediaAsync(string callId, string userId, ToggleMediaDto dto)
        {
            try
            {
                // In production, update CallParticipant entity
                _logger.LogInformation("User {UserId} toggled media in call {CallId} - Audio: {Audio}, Video: {Video}",
                    userId, callId, dto.AudioEnabled, dto.VideoEnabled);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling media");
                throw;
            }
        }

        public async Task<IPagedList<CallHistoryDto>> GetCallHistoryAsync(
          string userId,
          CallsType? callType,
          CallStatus? status,
          int pageNumber,
          int pageSize)
        {
            try
            {
                var query = _callRepository.Table
                    .Where(c => (c.CallerId == userId || c.ReceiverId == userId) && !c.IsDeleted);

                if (callType.HasValue)
                    query = query.Where(c => c.Type == callType.Value);

                if (status.HasValue)
                    query = query.Where(c => c.Status == status.Value);

                var totalCount = await query.CountAsync();

                var calls = await query
                    .OrderByDescending(c => c.InitiatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var callHistoryDtos = new List<CallHistoryDto>();

                foreach (var call in calls)
                {
                    var otherUserId = call.CallerId == userId ? call.ReceiverId : call.CallerId;
                    var otherUser = otherUserId != null
                        ? await _userRepository.GetByIdAsync(long.Parse(otherUserId))
                        : null;

                    callHistoryDtos.Add(new CallHistoryDto
                    {
                        CallId = call.Id.ToString(),
                        OtherUserId = otherUserId,
                        OtherUserName = otherUser?.DisplayName ?? otherUser?.Username,
                        OtherUserAvatar = otherUser?.ProfilePictureUrl,
                        Type = call.Type,
                        Status = call.Status,
                        Direction = call.CallerId == userId ? CallDirection.Outgoing : CallDirection.Incoming,
                        InitiatedAt = call.InitiatedAt,
                        ConnectedAt = call.ConnectedAt,
                        EndedAt = call.EndedAt,
                        DurationSeconds = call.DurationSeconds,
                        EndReason = call.EndReason
                    });
                }

                return new PagedList<CallHistoryDto>(callHistoryDtos, pageNumber, pageSize, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting call history for user {UserId}", userId);
                throw;
            }
        }

        public async Task<CallStatisticsDto> GetCallStatisticsAsync(
            string userId,
            DateTime fromDate,
            DateTime toDate)
        {
            try
            {
                var calls = await _callRepository.Table
                    .Where(c =>
                        (c.CallerId == userId || c.ReceiverId == userId) &&
                        c.InitiatedAt >= fromDate &&
                        c.InitiatedAt <= toDate &&
                        !c.IsDeleted)
                    .ToListAsync();

                var totalCalls = calls.Count;
                var incomingCalls = calls.Count(c => c.ReceiverId == userId);
                var outgoingCalls = calls.Count(c => c.CallerId == userId);
                var missedCalls = calls.Count(c => c.Status == CallStatus.Missed && c.ReceiverId == userId);
                var totalDuration = calls.Sum(c => c.DurationSeconds);
                var videoCalls = calls.Count(c => c.Type == CallsType.Video);
                var voiceCalls = calls.Count(c => c.Type == CallsType.Voice);

                var callsByDay = calls
                    .GroupBy(c => c.InitiatedAt.Date.ToString("yyyy-MM-dd"))
                    .ToDictionary(g => g.Key, g => g.Count());

                return new CallStatisticsDto
                {
                    TotalCalls = totalCalls,
                    IncomingCalls = incomingCalls,
                    OutgoingCalls = outgoingCalls,
                    MissedCalls = missedCalls,
                    TotalDurationSeconds = totalDuration,
                    AverageDurationSeconds = totalCalls > 0 ? totalDuration / totalCalls : 0,
                    VideoCalls = videoCalls,
                    VoiceCalls = voiceCalls,
                    CallsByDay = callsByDay
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting call statistics for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IList<CallHistoryDto>> GetMissedCallsAsync(
            string userId,
            int pageNumber,
            int pageSize)
        {
            try
            {
                var calls = await _callRepository.Table
                    .Where(c =>
                        c.ReceiverId == userId &&
                        c.Status == CallStatus.Missed &&
                        !c.IsDeleted)
                    .OrderByDescending(c => c.InitiatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var callHistoryDtos = new List<CallHistoryDto>();

                foreach (var call in calls)
                {
                    var caller = await _userRepository.GetByIdAsync(long.Parse(call.CallerId));

                    callHistoryDtos.Add(new CallHistoryDto
                    {
                        CallId = call.Id.ToString(),
                        OtherUserId = call.CallerId,
                        OtherUserName = caller?.DisplayName ?? caller?.Username,
                        OtherUserAvatar = caller?.ProfilePictureUrl,
                        Type = call.Type,
                        Status = call.Status,
                        Direction = CallDirection.Incoming,
                        InitiatedAt = call.InitiatedAt,
                        ConnectedAt = call.ConnectedAt,
                        EndedAt = call.EndedAt,
                        DurationSeconds = call.DurationSeconds,
                        EndReason = call.EndReason
                    });
                }

                return callHistoryDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting missed calls for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteCallHistoryAsync(string callId, string userId)
        {
            try
            {
                var call = await _callRepository.GetByIdAsync(long.Parse(callId));

                if (call == null)
                    throw new InvalidOperationException("Call not found");

                if (call.CallerId != userId && call.ReceiverId != userId)
                    throw new UnauthorizedAccessException("User is not authorized to delete this call");

                // Soft delete
                call.IsDeleted = true;
                call.UpdatedOnUtc = DateTime.UtcNow;
                call.DeletedBy = long.Parse(userId);

                await _callRepository.UpdateAsync(call);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Call {CallId} deleted by user {UserId}", callId, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting call history {CallId}", callId);
                throw;
            }
        }

        public async Task<Call?> GetCallAsync(string callId)
        {
            try
            {
                return await _callRepository.GetByIdAsync(long.Parse(callId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting call {CallId}", callId);
                throw;
            }
        }
    }
}