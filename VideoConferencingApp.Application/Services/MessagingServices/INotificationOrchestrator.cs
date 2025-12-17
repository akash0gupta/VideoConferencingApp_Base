using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;

namespace VideoConferencingApp.Application.Services.MessagingServices
{
    public interface INotificationOrchestrator
    {
        Task NotifyMessageAsync(string receiverId, string senderId, string senderName, string message, string messageId);
        Task NotifyIncomingCallAsync(string receiverId, string callerId, string callerName, string callerAvatar, string callId, CallsType callType);
        Task NotifyMissedCallAsync(string userId, string callerId, string callerName, string callId);
        Task NotifyContactRequestAsync(string receiverId, string requesterId, string requesterName, string requesterAvatar, long contactId);
        Task NotifyGroupMessageAsync(string groupId, List<string> memberIds, string senderId, string senderName, string message);
    }
}
