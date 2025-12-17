using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using VideoConferencingApp.Application.DTOs.Call;
using VideoConferencingApp.Application.DTOs.Chat;
using VideoConferencingApp.Application.DTOs.Group;
using VideoConferencingApp.Application.DTOs.Presence;
using VideoConferencingApp.Application.Services.MessagingServices;
using VideoConferencingApp.Application.Services.UserServices;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Infrastructure.Services;
using VideoConferencingApp.Infrastructure.Services.AuthServices;

namespace VideoConferencingApp.Infrastructure.RealTime.Hubs
{
    [Authorize]
    public class SignalsHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly IUserService _userService;
        private readonly ICallService _callService;
        private readonly IGroupService _groupService;
        private readonly IPresenceService _presenceService;
        private readonly IConnectionManagerService _connectionManager;
        private readonly IAuditLogger _auditLogger;
        private readonly ILogger<SignalsHub> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationOrchestrator _notificationOrchestrator;

        public SignalsHub(
            IChatService chatService,
            IUserService userService,
            ICallService callService,
            IGroupService groupService,
            IPresenceService presenceService,
            IConnectionManagerService connectionManager,
            IAuditLogger auditLogger,
            ILogger<SignalsHub> logger,
            IHttpContextAccessor httpContextAccessor,
            INotificationOrchestrator notificationOrchestrator)
        {
            _chatService = chatService;
            _callService = callService;
            _userService = userService;
            _groupService = groupService;
            _presenceService = presenceService;
            _connectionManager = connectionManager;
            _auditLogger = auditLogger;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _notificationOrchestrator = notificationOrchestrator;
        }

        #region Connection Lifecycle

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Anonymous connection attempted: {ConnectionId}", connectionId);
                Context.Abort();
                return;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
            var deviceId = httpContext?.Request.Query["deviceId"].ToString();

            // Add connection
            await _connectionManager.AddConnectionAsync(userId, connectionId, deviceId, userAgent, ipAddress);

            // Update presence
            await _presenceService.UpdatePresenceAsync(userId, new UpdatePresenceDto
            {
                Status = UserPresenceStatus.Online
            });

            // Get undelivered messages
            var undeliveredMessages = await _chatService.GetUndeliveredMessagesAsync(userId);
            if (undeliveredMessages.Any())
            {
                await Clients.Caller.SendAsync("UndeliveredMessages", undeliveredMessages);
            }

            // Join user groups
            var userGroups = await _groupService.GetUserGroupsAsync(userId);
            foreach (var group in userGroups)
            {
                await Groups.AddToGroupAsync(connectionId, group.GroupId);
            }

            // Notify contacts
            await NotifyPresenceChangeAsync(userId, UserPresenceStatus.Online);

            // Audit log
            await _auditLogger.LogAsync(new AuditLog
            {
                UserId = userId,
                Action = "SignalR.Connected",
                IpAddress = ipAddress ?? "Unknown",
                UserAgent = userAgent,
                Severity = AuditSeverity.Info,
                Details = "User connected to SignalR hub",
                Metadata = new Dictionary<string, object>
                {
                    { "ConnectionId", connectionId },
                    { "DeviceId", deviceId ?? "Unknown" }
                }
            });

            _logger.LogInformation("User {UserId} connected - ConnectionId: {ConnectionId}", userId, connectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            var connectionId = Context.ConnectionId;

            if (!string.IsNullOrEmpty(userId))
            {
                await _connectionManager.RemoveConnectionAsync(connectionId);

                // Check if user has other connections
                var isOnline = await _connectionManager.IsUserOnlineAsync(userId);

                if (!isOnline)
                {
                    await _presenceService.UpdatePresenceAsync(userId, new UpdatePresenceDto
                    {
                        Status = UserPresenceStatus.Offline
                    });
                    await NotifyPresenceChangeAsync(userId, UserPresenceStatus.Offline);
                }

                // Audit log
                await _auditLogger.LogAsync(new AuditLog
                {
                    UserId = userId,
                    Action = "SignalR.Disconnected",
                    IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    Severity = exception != null ? AuditSeverity.Warning : AuditSeverity.Info,
                    Details = exception != null ? $"Disconnected with error: {exception.Message}" : "Normal disconnection",
                    Metadata = new Dictionary<string, object>
                    {
                        { "ConnectionId", connectionId },
                        { "HasException", exception != null }
                    }
                });

                _logger.LogInformation("User {UserId} disconnected - Exception: {Exception}",
                    userId, exception?.Message ?? "None");
            }

            await base.OnDisconnectedAsync(exception);
        }

        #endregion

        #region Chat Methods

        /// <summary>
        /// Send a message
        /// </summary>
        public async Task SendMessageAsync(SendMessageDto dto)
        {
            var senderId = Context.UserIdentifier!;

            try
            {
                var message = await _chatService.SendMessageAsync(senderId, dto);

                if (!string.IsNullOrEmpty(dto.ReceiverId))
                {
                    var receiverConnections = await _connectionManager.GetUserConnectionsAsync(dto.ReceiverId);

                    if (receiverConnections.Any())
                    {
                        // User is connected - send via SignalR
                        message.Status = MessageStatus.Delivered;
                        await Clients.Clients(receiverConnections).SendAsync("ReceiveMessage", message);
                        await _chatService.MarkMessagesAsDeliveredAsync(message.MessageId, dto.ReceiverId);
                    }
                    else
                    {
                        // User is offline - trigger push notification
                        var sender = await _userService.GetByIdAsync(long.Parse(senderId));
                        await _notificationOrchestrator.NotifyMessageAsync(
                            receiverId: dto.ReceiverId,
                            senderId: senderId,
                            senderName: sender.DisplayName ?? sender.Username,
                            message: dto.Content,
                            messageId: message.MessageId
                        );
                    }

                    // Echo to sender's other devices
                    var senderConnections = await _connectionManager.GetUserConnectionsAsync(senderId);
                    var otherConnections = senderConnections.Where(c => c != Context.ConnectionId).ToList();
                    if (otherConnections.Any())
                    {
                        await Clients.Clients(otherConnections).SendAsync("MessageSent", message);
                    }
                }
                else if (!string.IsNullOrEmpty(dto.GroupId))
                {
                    // Group message
                    await Clients.OthersInGroup(dto.GroupId).SendAsync("ReceiveMessage", message);
                    await Clients.Caller.SendAsync("MessageSent", message);

                    // Get offline members and send push notifications
                    var group = await _groupService.GetGroupAsync(dto.GroupId);
                    var offlineMembers = new List<string>();

                    foreach (var member in group.Members)
                    {
                        if (member.UserId == senderId) continue;

                        var isOnline = await _connectionManager.IsUserOnlineAsync(member.UserId);
                        if (!isOnline)
                        {
                            offlineMembers.Add(member.UserId);
                        }
                    }

                    if (offlineMembers.Any())
                    {
                        var sender = await _userService.GetByIdAsync(long.Parse(senderId));
                        await _notificationOrchestrator.NotifyGroupMessageAsync(
                            groupId: dto.GroupId,
                            memberIds: offlineMembers,
                            senderId: senderId,
                            senderName: sender.DisplayName ?? sender.Username,
                            message: dto.Content
                        );
                    }
                }

                // Audit
                await _auditLogger.LogAsync(new AuditLog
                {
                    UserId = senderId,
                    TargetUserId = dto.ReceiverId,
                    Action = "Message.Sent",
                    IpAddress = GetIpAddress(),
                    Severity = AuditSeverity.Info,
                    Details = $"Message sent - Type: {dto.Type}",
                    Metadata = new Dictionary<string, object>
                    {
                        { "MessageId", message.MessageId },
                        { "Type", dto.Type.ToString() }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message from {SenderId}", senderId);
                await Clients.Caller.SendAsync("MessageError", new { Error = "Failed to send message" });
            }
        }

        /// <summary>
        /// Typing indicator
        /// </summary>
        public async Task SendTypingIndicatorAsync(TypingIndicatorDto dto)
        {
            var userId = Context.UserIdentifier!;
            dto.UserId = userId;

            try
            {
                if (!string.IsNullOrEmpty(dto.ChatId))
                {
                    var connections = await _connectionManager.GetUserConnectionsAsync(dto.ChatId);
                    await Clients.Clients(connections).SendAsync("UserTyping", dto);
                }
                else if (!string.IsNullOrEmpty(dto.GroupId))
                {
                    await Clients.OthersInGroup(dto.GroupId).SendAsync("UserTyping", dto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending typing indicator");
            }
        }

        /// <summary>
        /// Mark messages as read
        /// </summary>
        public async Task MarkMessagesAsReadAsync(MarkMessagesReadDto dto)
        {
            var userId = Context.UserIdentifier!;

            try
            {
                await _chatService.MarkMessagesAsReadAsync(dto.MessageIds, userId);

                var readDto = new
                {
                    MessageIds = dto.MessageIds,
                    UserId = userId,
                    ReadAt = DateTime.UtcNow
                };

                // Notify sender
                if (!string.IsNullOrEmpty(dto.ChatId))
                {
                    var connections = await _connectionManager.GetUserConnectionsAsync(dto.ChatId);
                    await Clients.Clients(connections).SendAsync("MessagesRead", readDto);
                }
                else if (!string.IsNullOrEmpty(dto.GroupId))
                {
                    await Clients.OthersInGroup(dto.GroupId).SendAsync("MessagesRead", readDto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking messages as read");
            }
        }

        #endregion

        #region Call Methods

        /// <summary>
        /// Initiate call
        /// </summary>
        public async Task InitiateCallAsync(InitiateCallDto dto)
        {
            var callerId = Context.UserIdentifier!;

            try
            {
                // Create call
                var call = await _callService.InitiateCallAsync(callerId, dto);

                // Check if receiver is online
                if (!string.IsNullOrEmpty(dto.ReceiverId))
                {
                    var isOnline = await _connectionManager.IsUserOnlineAsync(dto.ReceiverId);

                    if (!isOnline)
                    {
                        await Clients.Caller.SendAsync("CallFailed", new { call.CallId, Reason = "User is offline" });

                            // User is offline - send push notification
                            await _notificationOrchestrator.NotifyIncomingCallAsync(
                                receiverId: dto.ReceiverId,
                                callerId: callerId,
                                callerName: call.CallerName,
                                callerAvatar: call.CallerAvatar,
                                callId: call.CallId,
                                callType: dto.CallType
                            );

                        return;
                    }

                    // Notify receiver
                    var receiverConnections = await _connectionManager.GetUserConnectionsAsync(dto.ReceiverId);
                    await Clients.Clients(receiverConnections).SendAsync("IncomingCall", call);
                }
                else if (!string.IsNullOrEmpty(dto.GroupId))
                {
                    // Group call
                    await Clients.OthersInGroup(dto.GroupId).SendAsync("IncomingCall", call);
                }

                // Audit
                await _auditLogger.LogAsync(new AuditLog
                {
                    UserId = callerId,
                    TargetUserId = dto.ReceiverId,
                    Action = $"Call.Initiated.{dto.CallType}",
                    IpAddress = GetIpAddress(),
                    Severity = AuditSeverity.Info,
                    Details = $"{dto.CallType} call initiated",
                    Metadata = new Dictionary<string, object>
                    {
                        { "CallId", call.CallId },
                        { "CallType", dto.CallType.ToString() }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating call");
                await Clients.Caller.SendAsync("CallFailed", new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Accept call
        /// </summary>
        public async Task AcceptCallAsync(string callId)
        {
            var userId = Context.UserIdentifier!;

            try
            {
                await _callService.AcceptCallAsync(callId, userId);

                var response = new CallResponseDto
                {
                    CallId = callId,
                    Accepted = true
                };

                // Notify caller (you'll need to get caller ID from call entity)
                await Clients.Others.SendAsync("CallAccepted", response);

                await _auditLogger.LogAsync(new AuditLog
                {
                    UserId = userId,
                    Action = "Call.Accepted",
                    IpAddress = GetIpAddress(),
                    Severity = AuditSeverity.Info,
                    Details = "Call accepted",
                    Metadata = new Dictionary<string, object> { { "CallId", callId } }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting call {CallId}", callId);
                await Clients.Caller.SendAsync("CallError", new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Reject call
        /// </summary>
        public async Task RejectCallAsync(string callId, string? reason)
        {
            var userId = Context.UserIdentifier!;

            try
            {
                await _callService.RejectCallAsync(callId, userId, reason);

                var response = new CallResponseDto
                {
                    CallId = callId,
                    Accepted = false,
                    Reason = reason
                };

                await Clients.Others.SendAsync("CallRejected", response);

                await _auditLogger.LogAsync(new AuditLog
                {
                    UserId = userId,
                    Action = "Call.Rejected",
                    IpAddress = GetIpAddress(),
                    Severity = AuditSeverity.Info,
                    Details = $"Call rejected - Reason: {reason ?? "N/A"}",
                    Metadata = new Dictionary<string, object> { { "CallId", callId } }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting call {CallId}", callId);
            }
        }

        /// <summary>
        /// End call
        /// </summary>
        public async Task EndCallAsync(string callId)
        {
            var userId = Context.UserIdentifier!;

            try
            {
                var result = await _callService.EndCallAsync(callId, userId);

                await Clients.Others.SendAsync("CallEnded", result);

                await _auditLogger.LogAsync(new AuditLog
                {
                    UserId = userId,
                    Action = "Call.Ended",
                    IpAddress = GetIpAddress(),
                    Severity = AuditSeverity.Info,
                    Details = $"Call ended - Duration: {result.DurationSeconds}s",
                    Metadata = new Dictionary<string, object>
                    {
                        { "CallId", callId },
                        { "DurationSeconds", result.DurationSeconds }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending call {CallId}", callId);
            }
        }

        /// <summary>
        /// WebRTC signaling
        /// </summary>
        public async Task SendSignalAsync(WebRtcSignalDto signal)
        {
            var userId = Context.UserIdentifier!;
            signal.FromUserId = userId;

            try
            {
                var connections = await _connectionManager.GetUserConnectionsAsync(signal.ToUserId);
                await Clients.Clients(connections).SendAsync("ReceiveSignal", signal);

                _logger.LogDebug("Signal {Type} sent from {From} to {To} for call {CallId}",
                    signal.Type, signal.FromUserId, signal.ToUserId, signal.CallId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WebRTC signal");
            }
        }

        /// <summary>
        /// Toggle audio/video
        /// </summary>
        public async Task ToggleMediaAsync(ToggleMediaDto dto)
        {
            var userId = Context.UserIdentifier!;

            try
            {
                await _callService.ToggleMediaAsync(dto.CallId, userId, dto);

                await Clients.Others.SendAsync("MediaToggled", new
                {
                    CallId = dto.CallId,
                    UserId = userId,
                    AudioEnabled = dto.AudioEnabled,
                    VideoEnabled = dto.VideoEnabled
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling media");
            }
        }

        #endregion

        #region Group Methods

        /// <summary>
        /// Create group
        /// </summary>
        public async Task CreateGroupAsync(CreateGroupDto dto)
        {
            var userId = Context.UserIdentifier!;

            try
            {
                var group = await _groupService.CreateGroupAsync(userId, dto);

                // Add creator and members to SignalR group
                await Groups.AddToGroupAsync(Context.ConnectionId, group.GroupId);

                foreach (var member in group.Members)
                {
                    var connections = await _connectionManager.GetUserConnectionsAsync(member.UserId);
                    foreach (var conn in connections)
                    {
                        await Groups.AddToGroupAsync(conn, group.GroupId);
                    }
                }

                // Notify all members
                await Clients.Group(group.GroupId).SendAsync("GroupCreated", group);

                await _auditLogger.LogAsync(new AuditLog
                {
                    UserId = userId,
                    Action = "Group.Created",
                    IpAddress = GetIpAddress(),
                    Severity = AuditSeverity.Info,
                    Details = $"Group created: {dto.GroupName}",
                    Metadata = new Dictionary<string, object>
                    {
                        { "GroupId", group.GroupId },
                        { "MemberCount", dto.MemberIds.Count }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group");
                await Clients.Caller.SendAsync("GroupError", new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Join group
        /// </summary>
        public async Task JoinGroupAsync(string groupId)
        {
            var userId = Context.UserIdentifier!;

            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupId);

                await Clients.OthersInGroup(groupId).SendAsync("UserJoinedGroup", new
                {
                    GroupId = groupId,
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                });

                _logger.LogInformation("User {UserId} joined group {GroupId}", userId, groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining group {GroupId}", groupId);
            }
        }

        /// <summary>
        /// Leave group
        /// </summary>
        public async Task LeaveGroupAsync(string groupId)
        {
            var userId = Context.UserIdentifier!;

            try
            {
                await _groupService.LeaveGroupAsync(groupId, userId);

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupId);

                await Clients.OthersInGroup(groupId).SendAsync("UserLeftGroup", new
                {
                    GroupId = groupId,
                    UserId = userId,
                    LeftAt = DateTime.UtcNow
                });

                _logger.LogInformation("User {UserId} left group {GroupId}", userId, groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving group {GroupId}", groupId);
            }
        }

        #endregion

        #region Presence Methods

        /// <summary>
        /// Update presence
        /// </summary>
        public async Task UpdatePresenceAsync(UpdatePresenceDto dto)
        {
            var userId = Context.UserIdentifier!;

            try
            {
                await _presenceService.UpdatePresenceAsync(userId, dto);
                await NotifyPresenceChangeAsync(userId, dto.Status);

                _logger.LogInformation("User {UserId} updated presence to {Status}", userId, dto.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating presence");
            }
        }

        #endregion

        #region Helper Methods

        private async Task NotifyPresenceChangeAsync(string userId, UserPresenceStatus status)
        {
            try
            {
                var presence = await _presenceService.GetUserPresenceAsync(userId);
                if (presence != null)
                {
                    // Notify user's contacts (in production, get from contact service)
                    await Clients.Others.SendAsync("UserPresenceChanged", presence);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error notifying presence change");
            }
        }

        private string GetIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        #endregion
    }
}