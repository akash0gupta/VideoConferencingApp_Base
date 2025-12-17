using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.DTOs.Chat;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.Services.MessagingServices
{
    public interface IChatService
    {
        Task<ChatMessageDto> SendMessageAsync(string senderId, SendMessageDto dto);
        Task<IList<ChatMessageDto>> GetChatHistoryAsync(string userId, ChatHistoryDto dto);
        Task MarkMessagesAsDeliveredAsync(string messageId, string userId);
        Task MarkMessagesAsReadAsync(List<string> messageIds, string userId);
        Task<IList<ChatMessageDto>> GetUndeliveredMessagesAsync(string userId);
        Task<IList<ConversationDto>> GetConversationsAsync(string userId, int pageNumber, int pageSize);
        Task<IList<ChatMessageDto>> SearchMessagesAsync(string userId, string query, string? otherUserId, string? groupId, int pageNumber, int pageSize);
        Task<UnreadCountDto> GetUnreadCountAsync(string userId);
        Task<IList<GroupMediaDto>> GetGroupMediaAsync(string groupId, string userId, MediaType? mediaType, int pageNumber, int pageSize);
        Task<IList<SharedFileDto>> GetGroupSharedFilesAsync(string groupId, string userId, int pageNumber, int pageSize);
    }
}
