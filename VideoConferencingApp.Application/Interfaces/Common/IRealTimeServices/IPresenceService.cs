using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.Interfaces.Common.IRealTimeServices
{
    public interface IPresenceService
    {
        /// <summary>
        /// Marks a user as online and tracks their SignalR connection ID.
        /// </summary>
        Task UserConnectedAsync(long userId, string connectionId);

        /// <summary>
        /// Marks a user as offline and removes their SignalR connection ID.
        /// </summary>
        Task UserDisconnectedAsync(long userId, string connectionId);

        /// <summary>
        /// Checks if a user is currently considered online.
        /// </summary>
        Task<bool> IsOnlineAsync(long userId);

        /// <summary>
        /// Gets a list of all currently online user IDs.
        /// </summary>
        Task<long[]> GetOnlineUserIdsAsync();
    }
}
