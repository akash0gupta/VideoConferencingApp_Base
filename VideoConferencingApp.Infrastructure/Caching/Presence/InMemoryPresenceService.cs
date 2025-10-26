using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.IRealTimeServices;

namespace VideoConferencingApp.Infrastructure.Caching.Presence
{
    public class InMemoryPresenceService : IPresenceService
    {
        private readonly ConcurrentDictionary<long, HashSet<string>> _userConnections = new();
        private readonly ConcurrentDictionary<string, long> _connectionToUser = new();

        public Task UserConnectedAsync(long userId, string connectionId)
        {
            // Add connection to user's connection set
            _userConnections.AddOrUpdate(userId,
                new HashSet<string> { connectionId },
                (key, existingSet) =>
                {
                    existingSet.Add(connectionId);
                    return existingSet;
                });

            // Map connection back to user
            _connectionToUser[connectionId] = userId;

            return Task.CompletedTask;
        }

        public Task UserDisconnectedAsync(long userId, string connectionId)
        {
            // Remove the connection from the user's set
            if (_userConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(connectionId);

                // If user has no more connections, remove them entirely
                if (connections.Count == 0)
                {
                    _userConnections.TryRemove(userId, out _);
                }
            }

            // Remove connection mapping
            _connectionToUser.TryRemove(connectionId, out _);

            return Task.CompletedTask;
        }

        public Task<bool> IsOnlineAsync(long userId)
        {
            return Task.FromResult(_userConnections.ContainsKey(userId));
        }

        public Task<long[]> GetOnlineUserIdsAsync()
        {
            return Task.FromResult(_userConnections.Keys.ToArray());
        }

        // Additional helper method to get user by connection ID
        public Task<long?> GetUserIdByConnectionAsync(string connectionId)
        {
            if (_connectionToUser.TryGetValue(connectionId, out var userId))
            {
                return Task.FromResult<long?>(userId);
            }
            return Task.FromResult<long?>(null);
        }
    }
}
