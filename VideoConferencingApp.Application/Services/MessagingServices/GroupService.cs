using LinqToDB;
using Mapster;
using Microsoft.Extensions.Logging;
using VideoConferencingApp.Application.Common.ICommonServices;
using VideoConferencingApp.Application.DTOs.Group;
using VideoConferencingApp.Application.Services.MessagingServices;
using VideoConferencingApp.Domain.Entities.ChatEntities;
using VideoConferencingApp.Domain.Entities.UserEntities;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Interfaces;

namespace VideoConferencingApp.Application.Services.MessagingServices
{
    public class GroupService : IGroupService
    {
        private readonly IRepository<GroupMember> _groupMemberRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GroupService> _logger;
        private readonly IRepository<ChatGroup> _groupRepository;

    public GroupService(
      IRepository<ChatGroup> groupRepository,
      IRepository<GroupMember> groupMemberRepository,
      IRepository<User> userRepository,
      IUnitOfWork unitOfWork,
      ILogger<GroupService> logger)
        {
            _groupRepository = groupRepository;
            _groupMemberRepository = groupMemberRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<GroupDto> CreateGroupAsync(string createdBy, CreateGroupDto dto)
        {
            try
            {
                // Validate group name
                if (string.IsNullOrWhiteSpace(dto.GroupName))
                    throw new ArgumentException("Group name is required");

                if (dto.MemberIds == null || !dto.MemberIds.Any())
                    throw new ArgumentException("At least one member is required");

                // Check if creator exists
                var creator = await _userRepository.GetByIdAsync(long.Parse(createdBy));
                if (creator == null)
                    throw new InvalidOperationException("Creator user not found");

                // Validate all member IDs
                var memberUserIds = dto.MemberIds.Select(long.Parse).ToList();
                var users = await _userRepository.FindAsync(u =>
                    memberUserIds.Contains(u.Id) && u.IsActive && !u.IsDeleted);

                if (users.Count != dto.MemberIds.Count)
                    throw new InvalidOperationException("One or more member users not found or inactive");

                // Create group entity
                var group = new ChatGroup
                {
                    GroupName = dto.GroupName,
                    Description = dto.Description,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _groupRepository.InsertAsync(group);
                await _unitOfWork.SaveChangesAsync();

                // Add creator as owner
                var ownerMember = new GroupMember
                {
                    GroupId = group.Id.ToString(),
                    UserId = createdBy,
                    Role = GroupRole.Owner,
                    JoinedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _groupMemberRepository.InsertAsync(ownerMember);

                // Add other members
                foreach (var memberId in dto.MemberIds)
                {
                    if (memberId == createdBy) continue; // Skip creator, already added

                    var member = new GroupMember
                    {
                        GroupId = group.Id.ToString(),
                        UserId = memberId,
                        Role = GroupRole.Member,
                        JoinedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _groupMemberRepository.InsertAsync(member);
                }

                await _unitOfWork.SaveChangesAsync();

                // Build result DTO
                var groupDto = await BuildGroupDtoAsync(group);

                _logger.LogInformation(
                    "Group {GroupId} created by {CreatedBy} with {MemberCount} members",
                    group.Id, createdBy, dto.MemberIds.Count + 1);

                return groupDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group - CreatedBy: {CreatedBy}", createdBy);
                throw;
            }
        }

        public async Task<GroupDto?> GetGroupAsync(string groupId)
        {
            try
            {
                var group = await _groupRepository.Table
                   .FirstOrDefaultAsync(g => g.Id.ToString() == groupId && !g.IsDeleted);
                if (group == null || group.IsDeleted)
                    return null;

                return await BuildGroupDtoAsync(group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group {GroupId}", groupId);
                throw;
            }
        }

        public async Task<IList<GroupDto>> GetUserGroupsAsync(string userId)
        {
            try
            {
                var groups = from gr in _groupRepository.Table
                             from gm in _groupMemberRepository.Table.Where(gm => gm.GroupId == gr.Id.ToString())
                             where gm.UserId == userId && !gm.IsDeleted
                             select gr;

                var groupDtos = new List<GroupDto>();
                foreach (var group in groups)
                {
                    if (!group.IsDeleted)
                    {
                        groupDtos.Add(await BuildGroupDtoAsync(group));
                    }
                }

                _logger.LogDebug("Retrieved {Count} groups for user {UserId}", groupDtos.Count, userId);

                return groupDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting groups for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> AddMembersAsync(string groupId, string requesterId, AddMembersDto dto)
        {
            try
            {
                // Verify group exists
                var group = await _groupRepository.GetByIdAsync(long.Parse(groupId));
                if (group == null || group.IsDeleted)
                    throw new InvalidOperationException("Group not found");

                // Check if requester has permission (must be owner or admin)
                var requesterMember = await _groupMemberRepository.Table
                    .Where(m => m.GroupId == groupId && m.UserId == requesterId && !m.IsDeleted)
                    .FirstOrDefaultAsync();

                if (requesterMember == null)
                    throw new UnauthorizedAccessException("You are not a member of this group");

                if (requesterMember.Role != GroupRole.Owner && requesterMember.Role != GroupRole.Admin)
                    throw new UnauthorizedAccessException("Only owners and admins can add members");

                // Validate new member IDs
                var memberUserIds = dto.MemberIds.Select(long.Parse).ToList();
                var users = await _userRepository.FindAsync(u =>
                    memberUserIds.Contains(u.Id) && u.IsActive && !u.IsDeleted);

                if (users.Count != dto.MemberIds.Count)
                    throw new InvalidOperationException("One or more users not found or inactive");

                // Check if already members
                var existingMembers = await _groupMemberRepository.Table
                    .Where(m => m.GroupId == groupId && dto.MemberIds.Contains(m.UserId) && !m.IsDeleted)
                    .Select(m => m.UserId)
                    .ToListAsync();

                // Add new members
                var addedCount = 0;
                foreach (var memberId in dto.MemberIds)
                {
                    if (existingMembers.Contains(memberId))
                    {
                        _logger.LogWarning("User {UserId} is already a member of group {GroupId}", memberId, groupId);
                        continue;
                    }

                    var member = new GroupMember
                    {
                        GroupId = groupId,
                        UserId = memberId,
                        Role = GroupRole.Member,
                        JoinedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };

                    await _groupMemberRepository.InsertAsync(member);
                    addedCount++;
                }

                if (addedCount > 0)
                {
                    await _unitOfWork.SaveChangesAsync();
                }

                _logger.LogInformation(
                    "Added {Count} members to group {GroupId} by {RequesterId}",
                    addedCount, groupId, requesterId);

                return addedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding members to group {GroupId}", groupId);
                throw;
            }
        }

        public async Task<bool> RemoveMemberAsync(string groupId, string requesterId, RemoveMemberDto dto)
        {
            try
            {
                // Verify group exists
                var group = await _groupRepository.GetByIdAsync(long.Parse(groupId));
                if (group == null || group.IsDeleted)
                    throw new InvalidOperationException("Group not found");

                // Check if requester has permission
                var requesterMember = await _groupMemberRepository.Table
                    .Where(m => m.GroupId == groupId && m.UserId == requesterId && !m.IsDeleted)
                    .FirstOrDefaultAsync();

                if (requesterMember == null)
                    throw new UnauthorizedAccessException("You are not a member of this group");

                // Get target member
                var targetMember = await _groupMemberRepository.Table
                    .Where(m => m.GroupId == groupId && m.UserId == dto.UserId && !m.IsDeleted)
                    .FirstOrDefaultAsync();

                if (targetMember == null)
                    throw new InvalidOperationException("User is not a member of this group");

                // Permission checks
                if (targetMember.Role == GroupRole.Owner)
                    throw new UnauthorizedAccessException("Cannot remove group owner");

                if (requesterMember.Role == GroupRole.Member)
                    throw new UnauthorizedAccessException("Members cannot remove other members");

                if (requesterMember.Role == GroupRole.Admin && targetMember.Role == GroupRole.Admin && requesterId != dto.UserId)
                    throw new UnauthorizedAccessException("Admins cannot remove other admins");

                // Soft delete the member
                targetMember.IsDeleted = true;
                targetMember.UpdatedOnUtc = DateTime.UtcNow;
                targetMember.DeletedBy = long.Parse(requesterId);

                await _groupMemberRepository.UpdateAsync(targetMember);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Member {UserId} removed from group {GroupId} by {RequesterId}",
                    dto.UserId, groupId, requesterId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member from group {GroupId}", groupId);
                throw;
            }
        }

        public async Task<bool> LeaveGroupAsync(string groupId, string userId)
        {
            try
            {
                // Verify group exists
                var group = await _groupRepository.GetByIdAsync(long.Parse(groupId));
                if (group == null || group.IsDeleted)
                    throw new InvalidOperationException("Group not found");

                // Get user's membership
                var member = await _groupMemberRepository.Table
                    .Where(m => m.GroupId == groupId && m.UserId == userId && !m.IsDeleted)
                    .FirstOrDefaultAsync();

                if (member == null)
                    throw new InvalidOperationException("You are not a member of this group");

                // Check if user is owner
                if (member.Role == GroupRole.Owner)
                {
                    // Check if there are other members
                    var memberCount = await _groupMemberRepository.Table
                        .Where(m => m.GroupId == groupId && !m.IsDeleted)
                        .CountAsync();

                    if (memberCount > 1)
                    {
                        // Transfer ownership to first admin, or first member
                        var newOwner = await _groupMemberRepository.Table
                            .Where(m => m.GroupId == groupId && m.UserId != userId && !m.IsDeleted)
                            .OrderBy(m => m.Role == GroupRole.Admin ? 0 : 1)
                            .ThenBy(m => m.JoinedAt)
                            .FirstOrDefaultAsync();

                        if (newOwner != null)
                        {
                            newOwner.Role = GroupRole.Owner;
                            await _groupMemberRepository.UpdateAsync(newOwner);

                            _logger.LogInformation(
                                "Ownership transferred from {OldOwner} to {NewOwner} in group {GroupId}",
                                userId, newOwner.UserId, groupId);
                        }
                    }
                    else
                    {
                        // Last member - delete the group
                        group.IsDeleted = true;
                        group.UpdatedOnUtc = DateTime.UtcNow;
                        group.DeletedBy = long.Parse(userId);
                        await _groupRepository.UpdateAsync(group);

                        _logger.LogInformation("Group {GroupId} deleted as last member left", groupId);
                    }
                }

                // Remove member
                member.IsDeleted = true;
                member.UpdatedOnUtc = DateTime.UtcNow;
                member.DeletedBy = long.Parse(userId);

                await _groupMemberRepository.UpdateAsync(member);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("User {UserId} left group {GroupId}", userId, groupId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving group {GroupId}", groupId);
                throw;
            }
        }

        public async Task<IList<GroupActivityDto>> GetGroupActivityAsync(
           string groupId,
           int pageNumber,
           int pageSize)
        {
            try
            {
                // In a real implementation, you'd have a GroupActivity table
                // For now, we'll derive activities from GroupMember history
                var memberChanges = await _groupMemberRepository.Table
                    .Where(m => m.GroupId == groupId)
                    .OrderByDescending(m => m.CreatedOnUtc)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var activities = new List<GroupActivityDto>();

                foreach (var member in memberChanges)
                {
                    var user = await _userRepository.GetByIdAsync(long.Parse(member.UserId));

                    GroupActivityType activityType;
                    string description;

                    if (member.IsDeleted)
                    {
                        activityType = GroupActivityType.MemberLeft;
                        description = $"{user?.Username ?? "Unknown"} left the group";
                    }
                    else
                    {
                        activityType = member.Role == GroupRole.Owner
                            ? GroupActivityType.Created
                            : GroupActivityType.MemberAdded;
                        description = activityType == GroupActivityType.Created
                            ? $"{user?.Username ?? "Unknown"} created the group"
                            : $"{user?.Username ?? "Unknown"} joined the group";
                    }

                    activities.Add(new GroupActivityDto
                    {
                        Id = member.Id,
                        Type = activityType,
                        Description = description,
                        UserId = member.UserId,
                        Username = user?.Username,
                        OccurredAt = member.CreatedOnUtc
                    });
                }

                return activities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group activity for group {GroupId}", groupId);
                throw;
            }
        }

        public async Task<IList<GroupMemberHistoryDto>> GetMemberHistoryAsync(
            string groupId,
            int pageNumber,
            int pageSize)
        {
            try
            {
                // Get all member records (including deleted) for history
                var members = await _groupMemberRepository.Table
                    .Where(m => m.GroupId == groupId)
                    .OrderByDescending(m => m.CreatedOnUtc)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var memberHistory = new List<GroupMemberHistoryDto>();

                foreach (var member in members)
                {
                    var user = await _userRepository.GetByIdAsync(long.Parse(member.UserId));

                    GroupMemberAction action;
                    DateTime actionDate;

                    if (member.IsDeleted)
                    {
                        action = GroupMemberAction.Left;
                        actionDate = member.UpdatedOnUtc ?? member.CreatedOnUtc;
                    }
                    else
                    {
                        action = member.Role == GroupRole.Owner
                            ? GroupMemberAction.Joined
                            : GroupMemberAction.Joined;
                        actionDate = member.JoinedAt;
                    }

                    memberHistory.Add(new GroupMemberHistoryDto
                    {
                        UserId = member.UserId,
                        Username = user?.Username ?? "Unknown",
                        Avatar = user?.ProfilePictureUrl,
                        Action = action,
                        ActionDate = actionDate,
                        ActionBy = member.DeletedBy.ToString()
                    });
                }

                return memberHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting member history for group {GroupId}", groupId);
                throw;
            }
        }

        #region Helper Methods

        private async Task<GroupDto> BuildGroupDtoAsync(ChatGroup group)
        {
            // Get group members
            var members = await _groupMemberRepository.Table
                .Where(m => m.GroupId == group.Id.ToString() && !m.IsDeleted)
                .ToListAsync();

            var memberUserIds = members.Select(m => long.Parse(m.UserId)).ToList();
            var users = await _userRepository.FindAsync(u => memberUserIds.Contains(u.Id));
            var userDict = users.ToDictionary(u => u.Id.ToString());

            var memberDtos = members.Select(m =>
            {
                var user = userDict.GetValueOrDefault(m.UserId);
                return new GroupMemberDto
                {
                    UserId = m.UserId,
                    Username = user?.Username ?? "Unknown",
                    Avatar = user?.ProfilePictureUrl,
                    Role = m.Role,
                    JoinedAt = m.JoinedAt
                };
            }).ToList();

            return new GroupDto
            {
                GroupId = group.Id.ToString(),
                GroupName = group.GroupName,
                Description = group.Description,
                AvatarUrl = group.AvatarUrl,
                CreatedBy = group.CreatedBy,
                CreatedAt = group.CreatedAt,
                Members = memberDtos
            };
        }
        #endregion
    }
}