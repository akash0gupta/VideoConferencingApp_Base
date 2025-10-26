using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Domain.Models;

namespace VideoConferencingApp.Application.Interfaces.Common.IRealTimeServices
{
    public interface IConnectionManager
    {
        void AddConnection(string userId, string username, string connectionId);
        void RemoveConnection(string connectionId);
        UserConnection? GetUserByConnectionId(string connectionId);
        UserConnection? GetUserByUserId(string userId);
        string? GetConnectionId(string userId);
        IEnumerable<UserConnection> GetAllConnectedUsers();
        void UpdateCallStatus(string userId, bool isInCall, string? callWithUserId = null);
        bool IsUserAvailable(string userId);
    }
}
