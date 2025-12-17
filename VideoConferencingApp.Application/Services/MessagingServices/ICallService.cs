using Microsoft.VisualBasic;
using VideoConferencingApp.Application.DTOs.Call;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Interfaces;

namespace VideoConferencingApp.Application.Services.MessagingServices
{
    public interface ICallService
    {
        Task<CallInitiatedDto> InitiateCallAsync(string callerId, InitiateCallDto dto);
        Task<bool> AcceptCallAsync(string callId, string userId);
        Task<bool> RejectCallAsync(string callId, string userId, string? reason);
        Task<CallEndedDto> EndCallAsync(string callId, string userId);
        Task<bool> ToggleMediaAsync(string callId, string userId, ToggleMediaDto dto);
        Task<IPagedList<CallHistoryDto>> GetCallHistoryAsync(string userId, CallsType? callType, CallStatus? status, int pageNumber, int pageSize);
        Task<CallStatisticsDto> GetCallStatisticsAsync(string userId, DateTime fromDate, DateTime toDate);
        Task<IList<CallHistoryDto>> GetMissedCallsAsync(string userId, int pageNumber, int pageSize);
        Task<bool> DeleteCallHistoryAsync(string callId, string userId);
    }
}
