using Confluent.Kafka;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;
using VideoConferencingApp.Application.Interfaces.Common.IRealTimeServices;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Models;
using VideoConferencingApp.Infrastructure.Services;

namespace VideoConferencingApp.Infrastructure.RealTime
{
    [Authorize] 
    public class SecureCallHub : Hub<ICallClient>
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IRateLimitService _rateLimitService;
        private readonly IInputValidator _inputValidator;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<SecureCallHub> _logger;

        public SecureCallHub(
            IConnectionManager connectionManager,
            IRateLimitService rateLimitService,
            IInputValidator inputValidator,
            IAuditLogger auditLogger,
            ILogger<SecureCallHub> logger)
        {
            _connectionManager = connectionManager;
            _rateLimitService = rateLimitService;
            _inputValidator = inputValidator;
            _auditLogger = auditLogger;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            var username = GetUsername();
            var ipAddress = GetClientIpAddress();

            // Check if user is banned
            if (await _rateLimitService.IsBannedAsync(userId))
            {
                _logger.LogWarning("Banned user {UserId} attempted to connect from {IpAddress}", userId, ipAddress);
                Context.Abort();
                return;
            }

            // Rate limit connections
            if (!await _rateLimitService.IsAllowedAsync(userId, "connect"))
            {
                _logger.LogWarning("User {UserId} exceeded connection rate limit", userId);
                await _rateLimitService.BanUserAsync(userId, TimeSpan.FromMinutes(15));
                Context.Abort();
                return;
            }

            await _rateLimitService.RecordAttemptAsync(userId, "connect");

            // Validate user credentials
            if (!_inputValidator.IsValidUserId(userId) || !_inputValidator.IsValidUsername(username))
            {
                _logger.LogWarning("Invalid user credentials for connection");
                Context.Abort();
                return;
            }

            _connectionManager.AddConnection(userId, username, Context.ConnectionId);

            await _auditLogger.LogAsync(new AuditLog
            {
                UserId = userId,
                Action = "UserConnected",
                IpAddress = ipAddress,
                Details = $"User {username} connected"
            });

            await Clients.Others.UserConnected(userId, username);
            await base.OnConnectedAsync();
        }

        public async Task InitiateCall(string toUserId, string sdpOffer)
        {
            var fromUserId = GetUserId();
            var fromUsername = GetUsername();
            var ipAddress = GetClientIpAddress();

            // Validate inputs
            if (!_inputValidator.IsValidUserId(toUserId) || !_inputValidator.IsValidSdp(sdpOffer))
            {
                await Clients.Caller.ReceiveError("Invalid input parameters");
                return;
            }

            // Rate limiting
            if (!await _rateLimitService.IsAllowedAsync(fromUserId, "call"))
            {
                await Clients.Caller.ReceiveError("Too many call attempts. Please wait.");

                await _auditLogger.LogAsync(new AuditLog
                {
                    UserId = fromUserId,
                    Action = "CallRateLimitExceeded",
                    IpAddress = ipAddress,
                    Severity = AuditSeverity.Warning
                });

                return;
            }

            await _rateLimitService.RecordAttemptAsync(fromUserId, "call");

            // Check if user is trying to call themselves
            if (fromUserId == toUserId)
            {
                await Clients.Caller.ReceiveError("Cannot call yourself");
                return;
            }

            // Check if target user is available
            if (!_connectionManager.IsUserAvailable(toUserId))
            {
                await Clients.Caller.ReceiveError("User is not available");
                await Clients.Caller.CallRejected(toUserId, "User is busy or offline");
                return;
            }

            var targetConnectionId = _connectionManager.GetConnectionId(toUserId);
            if (targetConnectionId == null)
            {
                await Clients.Caller.ReceiveError("User not found");
                return;
            }

            // Update call status
            _connectionManager.UpdateCallStatus(fromUserId, true, toUserId);
            _connectionManager.UpdateCallStatus(toUserId, true, fromUserId);

            // Audit log
            await _auditLogger.LogAsync(new AuditLog
            {
                UserId = fromUserId,
                Action = "CallInitiated",
                TargetUserId = toUserId,
                IpAddress = ipAddress,
                Details = $"Call initiated to {toUserId}"
            });

            // Send encrypted offer
            await Clients.Client(targetConnectionId).ReceiveCallOffer(
                fromUserId,
                fromUsername,
                sdpOffer
            );

            _logger.LogInformation(
                "Call initiated: {FromUser} -> {ToUser} from {IpAddress}",
                fromUserId, toUserId, ipAddress
            );
        }

        public async Task AnswerCall(string toUserId, string sdpAnswer)
        {
            var fromUserId = GetUserId();
            var ipAddress = GetClientIpAddress();

            // Validate inputs
            if (!_inputValidator.IsValidUserId(toUserId) || !_inputValidator.IsValidSdp(sdpAnswer))
            {
                await Clients.Caller.ReceiveError("Invalid input parameters");
                return;
            }

            var targetConnectionId = _connectionManager.GetConnectionId(toUserId);
            if (targetConnectionId == null)
            {
                await Clients.Caller.ReceiveError("Caller not found");
                return;
            }

            // Verify that there's an active call between these users
            var fromUser = _connectionManager.GetUserByUserId(fromUserId);
            if (fromUser?.CurrentCallWithUserId != toUserId)
            {
                await Clients.Caller.ReceiveError("No active call found");
                return;
            }

            await _auditLogger.LogAsync(new AuditLog
            {
                UserId = fromUserId,
                Action = "CallAnswered",
                TargetUserId = toUserId,
                IpAddress = ipAddress,
                Details = $"Call answered from {toUserId}"
            });

            await Clients.Client(targetConnectionId).CallAccepted(fromUserId, sdpAnswer);

            _logger.LogInformation(
                "Call answered: {FromUser} accepted call from {ToUser}",
                fromUserId, toUserId
            );
        }

        public async Task SendIceCandidate(string toUserId, IceCandidate candidate)
        {
            var fromUserId = GetUserId();

            // Validate inputs
            if (!_inputValidator.IsValidUserId(toUserId) ||
                !_inputValidator.IsValidIceCandidate(candidate.Candidate))
            {
                await Clients.Caller.ReceiveError("Invalid ICE candidate");
                return;
            }

            // Rate limit ICE candidates
            if (!await _rateLimitService.IsAllowedAsync(fromUserId, "ice"))
            {
                _logger.LogWarning("User {UserId} exceeded ICE candidate rate limit", fromUserId);
                return;
            }

            await _rateLimitService.RecordAttemptAsync(fromUserId, "ice");

            var targetConnectionId = _connectionManager.GetConnectionId(toUserId);
            if (targetConnectionId == null)
            {
                _logger.LogWarning("Cannot send ICE candidate: target user {ToUserId} not found", toUserId);
                return;
            }

            // Verify active call
            var fromUser = _connectionManager.GetUserByUserId(fromUserId);
            if (fromUser?.CurrentCallWithUserId != toUserId)
            {
                _logger.LogWarning("ICE candidate sent without active call");
                return;
            }

            await Clients.Client(targetConnectionId).ReceiveIceCandidate(fromUserId, candidate);
        }

        public async Task EndCall(string toUserId)
        {
            var fromUserId = GetUserId();
            var ipAddress = GetClientIpAddress();

            if (!_inputValidator.IsValidUserId(toUserId))
            {
                return;
            }

            var targetConnectionId = _connectionManager.GetConnectionId(toUserId);
            if (targetConnectionId != null)
            {
                await Clients.Client(targetConnectionId).CallEnded(fromUserId, CallEndReason.UserHangup);
            }

            _connectionManager.UpdateCallStatus(fromUserId, false);
            _connectionManager.UpdateCallStatus(toUserId, false);

            await _auditLogger.LogAsync(new AuditLog
            {
                UserId = fromUserId,
                Action = "CallEnded",
                TargetUserId = toUserId,
                IpAddress = ipAddress,
                Details = "Call ended by user"
            });

            _logger.LogInformation("Call ended: {FromUser} ended call with {ToUser}", fromUserId, toUserId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            var user = _connectionManager.GetUserByConnectionId(Context.ConnectionId);

            if (user != null)
            {
                if (user.IsInCall && !string.IsNullOrEmpty(user.CurrentCallWithUserId))
                {
                    var otherUserConnectionId = _connectionManager.GetConnectionId(user.CurrentCallWithUserId);
                    if (otherUserConnectionId != null)
                    {
                        await Clients.Client(otherUserConnectionId).CallEnded(
                            user.UserId,
                            CallEndReason.ConnectionLost
                        );

                        _connectionManager.UpdateCallStatus(user.CurrentCallWithUserId, false);
                    }
                }

                _connectionManager.RemoveConnection(Context.ConnectionId);
                await Clients.Others.UserDisconnected(user.UserId);

                await _auditLogger.LogAsync(new AuditLog
                {
                    UserId = userId,
                    Action = "UserDisconnected",
                    IpAddress = GetClientIpAddress(),
                    Details = exception != null ? $"Disconnected with exception: {exception.Message}" : "Normal disconnect"
                });
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Helper methods
        private string GetUserId()
        {
            return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new HubException("User ID not found");
        }

        private string GetUsername()
        {
            return Context.User?.FindFirst(ClaimTypes.Name)?.Value
                ?? throw new HubException("Username not found");
        }

        private string GetClientIpAddress()
        {
            return Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString()
                ?? "Unknown";
        }
    }
}
