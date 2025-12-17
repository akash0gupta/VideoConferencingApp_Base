using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VideoConferencingApp.Application.Common.INotificationServices;
using VideoConferencingApp.Application.DTOs.Notification;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Infrastructure.Configuration;
using VideoConferencingApp.Infrastructure.Configuration.Settings;


namespace VideoConferencingApp.Infrastructure.Services.NotificationServices
{
    public class FirebasePushNotificationService : IFirebasePushNotificationService
    {
        private readonly FirebaseSettings _settings;
        private readonly ILogger<FirebasePushNotificationService> _logger;

        public FirebasePushNotificationService(
            AppSettings settings,
            ILogger<FirebasePushNotificationService> logger)
        {
            _settings = settings.Get<FirebaseSettings>();
            _logger = logger;
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(_settings.JsonFilePath) && File.Exists(_settings.JsonFilePath))
                    {
                        FirebaseApp.Create(new AppOptions
                        {
                            Credential = GoogleCredential.FromFile(_settings.JsonFilePath)
                        });
                        _logger.LogInformation("Firebase initialized from JSON file");
                    }
                    else
                    {
                        var credential = new
                        {
                            type = "service_account",
                            project_id = _settings.ProjectId,
                            private_key = _settings.PrivateKey.Replace("\\n", "\n"),
                            client_email = _settings.ClientEmail
                        };

                        FirebaseApp.Create(new AppOptions
                        {
                            Credential = GoogleCredential.FromJson(
                                System.Text.Json.JsonSerializer.Serialize(credential))
                        });
                        _logger.LogInformation("Firebase initialized from configuration");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Firebase");
                    throw;
                }
            }
        }

        public async Task<NotificationResponse> SendToDeviceAsync(SingleDeviceNotificationRequest request)
        {
            try
            {
                var message = BuildFirebaseMessage(request);
                message.Token = request.DeviceToken;

                var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message);

                _logger.LogInformation(
                    "Notification sent successfully to device. MessageId: {MessageId}",
                    messageId);

                return new NotificationResponse
                {
                    Success = true,
                    MessageId = messageId
                };
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "Failed to send notification to device token: {Token}",
                    MaskToken(request.DeviceToken));

                return new NotificationResponse
                {
                    Success = false,
                    Error = ex.Message,
                    ErrorCode = MapErrorCode(ex.MessagingErrorCode).ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending notification");

                return new NotificationResponse
                {
                    Success = false,
                    Error = ex.Message,
                    ErrorCode = NotificationErrorCode.Unknown.ToString()
                };
            }
        }

        public async Task<BatchNotificationResponse> SendToMultipleDevicesAsync(
            MultiDeviceNotificationRequest request)
        {
            try
            {
                if (request.DeviceTokens == null || !request.DeviceTokens.Any())
                {
                    throw new ArgumentException("Device tokens list cannot be empty");
                }

                var uniqueTokens = request.DeviceTokens.Distinct()
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

                var multicastMessage = BuildMulticastMessage(request, uniqueTokens);

                var response = await FirebaseMessaging.DefaultInstance
                    .SendEachForMulticastAsync(multicastMessage);

                var batchResponse = MapBatchResponse(response, uniqueTokens);

                _logger.LogInformation(
                    "Multicast notification completed. Success: {Success}, Failure: {Failure}",
                    batchResponse.SuccessCount,
                    batchResponse.FailureCount);

                return batchResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send multicast notification");

                return new BatchNotificationResponse
                {
                    SuccessCount = 0,
                    FailureCount = request.DeviceTokens?.Count ?? 0,
                    TotalCount = request.DeviceTokens?.Count ?? 0
                };
            }
        }

        public async Task<NotificationResponse> SendToTopicAsync(TopicNotificationRequest request)
        {
            try
            {
                var message = BuildFirebaseMessage(request);
                message.Topic = request.Topic;

                var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message);

                _logger.LogInformation(
                    "Topic notification sent successfully. MessageId: {MessageId}, Topic: {Topic}",
                    messageId,
                    request.Topic);

                return new NotificationResponse
                {
                    Success = true,
                    MessageId = messageId
                };
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "Failed to send topic notification: {Topic}", request.Topic);

                return new NotificationResponse
                {
                    Success = false,
                    Error = ex.Message,
                    ErrorCode = MapErrorCode(ex.MessagingErrorCode).ToString()
                };
            }
        }

        public async Task<NotificationResponse> SendToConditionAsync(
            ConditionalNotificationRequest request)
        {
            try
            {
                var message = BuildFirebaseMessage(request);
                message.Condition = request.Condition;

                var messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message);

                _logger.LogInformation(
                    "Conditional notification sent successfully. MessageId: {MessageId}, Condition: {Condition}",
                    messageId,
                    request.Condition);

                return new NotificationResponse
                {
                    Success = true,
                    MessageId = messageId
                };
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "Failed to send conditional notification: {Condition}",
                    request.Condition);

                return new NotificationResponse
                {
                    Success = false,
                    Error = ex.Message,
                    ErrorCode = MapErrorCode(ex.MessagingErrorCode).ToString()
                };
            }
        }

        public async Task<TopicManagementResponseDto> SubscribeToTopicAsync(
            List<string> deviceTokens,
            string topic)
        {
            try
            {
                var response = await FirebaseMessaging.DefaultInstance
                    .SubscribeToTopicAsync(deviceTokens, topic);

                var managementResponse = new TopicManagementResponseDto
                {
                    Success = response.FailureCount == 0,
                    SuccessCount = response.SuccessCount,
                    FailureCount = response.FailureCount
                };

                if (response.FailureCount > 0)
                {
                    foreach (var error in response.Errors)
                    {
                        managementResponse.Errors.Add(new TopicOperationError
                        {
                            Index = error.Index,
                            DeviceToken = deviceTokens[error.Index],
                            Reason = error.Reason
                        });
                    }
                }

                _logger.LogInformation(
                    "Subscribe to topic '{Topic}' completed. Success: {Success}, Failure: {Failure}",
                    topic,
                    response.SuccessCount,
                    response.FailureCount);

                return managementResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to topic: {Topic}", topic);

                return new TopicManagementResponseDto
                {
                    Success = false,
                    FailureCount = deviceTokens.Count
                };
            }
        }

        public async Task<TopicManagementResponseDto> UnsubscribeFromTopicAsync(
            List<string> deviceTokens,
            string topic)
        {
            try
            {
                var response = await FirebaseMessaging.DefaultInstance
                    .UnsubscribeFromTopicAsync(deviceTokens, topic);

                var managementResponse = new TopicManagementResponseDto
                {
                    Success = response.FailureCount == 0,
                    SuccessCount = response.SuccessCount,
                    FailureCount = response.FailureCount
                };

                if (response.FailureCount > 0)
                {
                    foreach (var error in response.Errors)
                    {
                        managementResponse.Errors.Add(new TopicOperationError
                        {
                            Index = error.Index,
                            DeviceToken = deviceTokens[error.Index],
                            Reason = error.Reason
                        });
                    }
                }

                _logger.LogInformation(
                    "Unsubscribe from topic '{Topic}' completed. Success: {Success}, Failure: {Failure}",
                    topic,
                    response.SuccessCount,
                    response.FailureCount);

                return managementResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unsubscribe from topic: {Topic}", topic);

                return new TopicManagementResponseDto
                {
                    Success = false,
                    FailureCount = deviceTokens.Count
                };
            }
        }

        public async Task<BatchNotificationResponse> SendBatchAsync(
            List<SingleDeviceNotificationRequest> requests)
        {
            try
            {
                const int batchSize = 500;
                var messages = requests.Select(BuildFirebaseMessageWithToken).ToList();

                if (messages.Count > batchSize)
                {
                    return await SendLargeBatchAsync(messages, requests);
                }

                var response = await FirebaseMessaging.DefaultInstance.SendEachAsync(messages);

                return MapBatchResponse(
                    response,
                    requests.Select(r => r.DeviceToken).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send batch notifications");

                return new BatchNotificationResponse
                {
                    SuccessCount = 0,
                    FailureCount = requests.Count,
                    TotalCount = requests.Count
                };
            }
        }

        public async Task<bool> ValidateDeviceTokenAsync(string deviceToken)
        {
            try
            {
                // Send a dry-run message to validate the token
                var message = new Message
                {
                    Token = deviceToken,
                    Data = new Dictionary<string, string> { { "validation", "true" } }
                };

                // Using DryRun mode - doesn't actually send
                await FirebaseMessaging.DefaultInstance.SendAsync(message, dryRun: true);

                return true;
            }
            catch (FirebaseMessagingException)
            {
                return false;
            }
        }

        #region Private Helper Methods

        private Message BuildFirebaseMessage(PushNotificationRequest request)
        {
            var message = new Message
            {
                Notification = new Notification
                {
                    Title = request.Title,
                    Body = request.Body,
                    ImageUrl = request.ImageUrl
                },
                Data = request.Data
            };

            // Android configuration
            if (_settings.DefaultConfig.Android != null)
            {
                var androidConfig = _settings.DefaultConfig.Android;
                message.Android = new AndroidConfig
                {
                    Priority = _settings.DefaultConfig.Priority == Domain.Enums.NotificationPriority.High
                        ? Priority.High
                        : Priority.Normal,
                    Notification = new AndroidNotification
                    {
                        Title = request.Title,
                        Body = request.Body,
                        Icon = androidConfig.Icon,
                        Color = androidConfig.Color,
                        Sound = androidConfig.Sound,
                        ChannelId = androidConfig.ChannelId,
                        ClickAction = androidConfig.ClickAction,
                        NotificationCount = androidConfig.NotificationCount,
                        Sticky = androidConfig.Sticky ?? false,
                        ImageUrl = request.ImageUrl
                    },
                    TimeToLive = request.TimeToLive
                };
            }

            // Apple configuration
            if (_settings.DefaultConfig.Apple != null)
            {
                var apple = _settings.DefaultConfig.Apple;

                message.Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Alert = new ApsAlert
                        {
                            Title = request.Title,
                            Body = request.Body
                        },
                        Badge = apple.Badge,
                        Sound = apple.Sound,
                        ContentAvailable = apple.ContentAvailable ?? false,
                        MutableContent = apple.MutableContent ?? false,
                        Category = apple.Category,
                        ThreadId = apple.ThreadId
                    }
                };

                if (apple.CustomData != null)
                {
                    message.Apns.CustomData = apple.CustomData;
                }
            }

            // Web configuration
            if (_settings.DefaultConfig.Web != null)
            {
                var web = _settings.DefaultConfig.Web;
                message.Webpush = new WebpushConfig
                {
                    Notification = new WebpushNotification
                    {
                        Title = request.Title,
                        Body = request.Body,
                        Icon = web.Icon,
                        Badge = web.Badge,
                        Image = web.Image
                    }
                };
            }

            return message;
        }

        private Message BuildFirebaseMessageWithToken(SingleDeviceNotificationRequest request)
        {
            var message = BuildFirebaseMessage(request);
            message.Token = request.DeviceToken;
            return message;
        }

        private MulticastMessage BuildMulticastMessage(
            MultiDeviceNotificationRequest request,
            List<string> tokens)
        {
            var message = new MulticastMessage
            {
                Tokens = tokens,
                Notification = new Notification
                {
                    Title = request.Title,
                    Body = request.Body,
                    ImageUrl = request.ImageUrl
                },
                Data = request.Data
            };

            // Android configuration
            if (_settings.DefaultConfig.Android != null)
            {
                var androidConfig = _settings.DefaultConfig.Android;
                message.Android = new AndroidConfig
                {
                    Priority = request.Priority == Domain.Enums.NotificationPriority.High
                        ? Priority.High
                        : Priority.Normal,
                    Notification = new AndroidNotification
                    {
                        Title = request.Title,
                        Body = request.Body,
                        Icon = androidConfig.Icon,
                        Color = androidConfig.Color,
                        Sound = androidConfig.Sound,
                        ChannelId = androidConfig.ChannelId,
                        ImageUrl = request.ImageUrl
                    }
                };
            }

            // Apple configuration
            if (_settings.DefaultConfig.Apple != null)
            {
                var appleConfig = _settings.DefaultConfig.Apple;
                message.Apns = new ApnsConfig
                {
                    Aps = new Aps
                    {
                        Alert = new ApsAlert
                        {
                            Title = request.Title,
                            Body = request.Body
                        },
                        Badge = appleConfig.Badge,
                        Sound = appleConfig.Sound,
                        ContentAvailable = appleConfig.ContentAvailable ?? false
                    }
                };
            }

            return message;
        }

        private async Task<BatchNotificationResponse> SendLargeBatchAsync(
            List<Message> messages,
            List<SingleDeviceNotificationRequest> requests)
        {
            const int batchSize = 500;
            var batches = messages
                .Select((msg, idx) => new { msg, idx })
                .GroupBy(x => x.idx / batchSize)
                .Select(g => g.Select(x => x.msg).ToList())
                .ToList();

            var allResults = new List<SingleNotificationResult>();
            var successCount = 0;
            var failureCount = 0;

            for (int i = 0; i < batches.Count; i++)
            {
                var batch = batches[i];
                var response = await FirebaseMessaging.DefaultInstance.SendEachAsync(batch);

                var startIdx = i * batchSize;
                var tokens = requests.Skip(startIdx).Take(batch.Count)
                    .Select(r => r.DeviceToken).ToList();

                var batchResults = MapSingleResults(response, tokens);
                allResults.AddRange(batchResults);

                successCount += response.SuccessCount;
                failureCount += response.FailureCount;
            }

            return new BatchNotificationResponse
            {
                SuccessCount = successCount,
                FailureCount = failureCount,
                TotalCount = messages.Count,
                Results = allResults,
                FailedTokens = allResults.Where(r => !r.Success)
                    .Select(r => r.DeviceToken).ToList()
            };
        }

        private BatchNotificationResponse MapBatchResponse(
            BatchResponse response,
            List<string> tokens)
        {
            var results = MapSingleResults(response, tokens);

            return new BatchNotificationResponse
            {
                SuccessCount = response.SuccessCount,
                FailureCount = response.FailureCount,
                TotalCount = tokens.Count,
                Results = results,
                FailedTokens = results.Where(r => !r.Success).Select(r => r.DeviceToken).ToList()
            };
        }

        private List<SingleNotificationResult> MapSingleResults(
            BatchResponse response,
            List<string> tokens)
        {
            var results = new List<SingleNotificationResult>();

            for (int i = 0; i < response.Responses.Count; i++)
            {
                var sendResponse = response.Responses[i];
                var token = i < tokens.Count ? tokens[i] : "unknown";

                results.Add(new SingleNotificationResult
                {
                    Success = sendResponse.IsSuccess,
                    MessageId = sendResponse.MessageId,
                    DeviceToken = token,
                    Error = sendResponse.Exception?.Message,
                    ErrorCode = sendResponse.Exception != null
                        ? MapErrorCode(sendResponse.Exception.MessagingErrorCode)
                        : null
                });

                if (!sendResponse.IsSuccess)
                {
                    _logger.LogWarning(
                        "Failed to send to token {Token}. Error: {Error}",
                        MaskToken(token),
                        sendResponse.Exception?.Message);
                }
            }

            return results;
        }

        private NotificationErrorCode MapErrorCode(MessagingErrorCode? errorCode)
        {
            return errorCode switch
            {
                MessagingErrorCode.InvalidArgument => NotificationErrorCode.InvalidArgument,
                MessagingErrorCode.Unregistered => NotificationErrorCode.Unregistered,
                MessagingErrorCode.SenderIdMismatch => NotificationErrorCode.SenderIdMismatch,
                MessagingErrorCode.QuotaExceeded => NotificationErrorCode.QuotaExceeded,
                MessagingErrorCode.Unavailable => NotificationErrorCode.Unavailable,
                MessagingErrorCode.Internal => NotificationErrorCode.Internal,
                MessagingErrorCode.ThirdPartyAuthError => NotificationErrorCode.ThirdPartyAuthError,
                _ => NotificationErrorCode.Unknown
            };
        }

        private string MaskToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length < 10)
                return "***";

            return $"{token.Substring(0, 5)}...{token.Substring(token.Length - 5)}";
        }

        #endregion
    }
}
