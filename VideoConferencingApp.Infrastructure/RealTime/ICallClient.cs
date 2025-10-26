using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Models;

namespace VideoConferencingApp.Infrastructure.RealTime
{
    /// <summary>
    /// Defines client-side methods that the server can invoke
    /// </summary>
    public interface ICallClient
    {
        // Call signaling
        Task ReceiveCallOffer(string fromUserId, string fromUsername, string sdpOffer);
        Task CallAccepted(string fromUserId, string sdpAnswer);
        Task CallRejected(string fromUserId, string reason);
        Task CallEnded(string fromUserId, CallEndReason reason);

        // WebRTC ICE candidates
        Task ReceiveIceCandidate(string fromUserId, IceCandidate candidate);

        // User presence
        Task UserConnected(string userId, string username);
        Task UserDisconnected(string userId);
        Task UserStatusChanged(string userId, bool isInCall);

        // Errors
        Task ReceiveError(string message);
    }
}
