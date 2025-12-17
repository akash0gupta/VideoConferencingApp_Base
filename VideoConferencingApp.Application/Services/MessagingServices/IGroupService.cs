using VideoConferencingApp.Application.DTOs.Group;

namespace VideoConferencingApp.Application.Services.MessagingServices
{
    public interface IGroupService
    {
        Task<GroupDto> CreateGroupAsync(string createdBy, CreateGroupDto dto);
        Task<GroupDto?> GetGroupAsync(string groupId);
        Task<IList<GroupDto>> GetUserGroupsAsync(string userId);
        Task<bool> AddMembersAsync(string groupId, string requesterId, AddMembersDto dto);
        Task<bool> RemoveMemberAsync(string groupId, string requesterId, RemoveMemberDto dto);
        Task<bool> LeaveGroupAsync(string groupId, string userId);
        Task<IList<GroupActivityDto>> GetGroupActivityAsync(string groupId, int pageNumber, int pageSize);
        Task<IList<GroupMemberHistoryDto>> GetMemberHistoryAsync(string groupId, int pageNumber, int pageSize);
    }
}
