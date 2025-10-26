using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.IRealTimeServices;
using VideoConferencingApp.Domain.Models;

namespace VideoConferencingApp.Infrastructure.Services
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly ConcurrentDictionary<string, UserConnection> _connections = new();
        private readonly ConcurrentDictionary<string, string> _userIdToConnectionId = new();
        private readonly ILogger<ConnectionManager> _logger;

        public ConnectionManager(ILogger<ConnectionManager> logger)
        {
            _logger = logger;
        }

        public void AddConnection(string userId, string username, string connectionId)
        {
            var connection = new UserConnection
            {
                UserId = userId,
                Username = username,
                ConnectionId = connectionId,
                ConnectedAt = DateTime.UtcNow,
                IsInCall = false
            };

            _connections[connectionId] = connection;
            _userIdToConnectionId[userId] = connectionId;

            _logger.LogInformation(
                "User {Username} ({UserId}) connected with connection ID {ConnectionId}",
                username, userId, connectionId
            );
        }

        public void RemoveConnection(string connectionId)
        {
            if (_connections.TryRemove(connectionId, out var connection))
            {
                _userIdToConnectionId.TryRemove(connection.UserId, out _);

                _logger.LogInformation(
                    "User {Username} ({UserId}) disconnected",
                    connection.Username, connection.UserId
                );
            }
        }

        public UserConnection? GetUserByConnectionId(string connectionId)
        {
            _connections.TryGetValue(connectionId, out var connection);
            return connection;
        }

        public UserConnection? GetUserByUserId(string userId)
        {
            if (_userIdToConnectionId.TryGetValue(userId, out var connectionId))
            {
                return GetUserByConnectionId(connectionId);
            }
            return null;
        }

        public string? GetConnectionId(string userId)
        {
            _userIdToConnectionId.TryGetValue(userId, out var connectionId);
            return connectionId;
        }

        public IEnumerable<UserConnection> GetAllConnectedUsers()
        {
            return _connections.Values.ToList();
        }

        public void UpdateCallStatus(string userId, bool isInCall, string? callWithUserId = null)
        {
            var user = GetUserByUserId(userId);
            if (user != null)
            {
                user.IsInCall = isInCall;
                user.CurrentCallWithUserId = callWithUserId;

                _logger.LogInformation(
                    "Updated call status for {UserId}: InCall={IsInCall}, With={CallWithUserId}",
                    userId, isInCall, callWithUserId
                );
            }
        }

        public bool IsUserAvailable(string userId)
        {
            var user = GetUserByUserId(userId);
            return user != null && !user.IsInCall;
        }
    }
}
