using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Infrastructure.Configuration.Settings;
using VideoConferencingApp.Infrastructure.Messaging.Kafka;
using VideoConferencingApp.Infrastructure.Messaging.RabbitMq;

namespace VideoConferencingApp.Infrastructure.Configuration.HealthChecks
{

    public class MessageBrokerHealthCheck : IHealthCheck
    {
        private readonly RabbitMqConnection ? _rabbitMqConnection;
        private readonly KafkaConnection ? _kafkaConnection;
        private readonly MessageBrokerSettings _settings;
        private readonly ILogger<MessageBrokerHealthCheck> _logger;

        public MessageBrokerHealthCheck(
            IServiceProvider serviceProvider,
            AppSettings appSettings,
            ILogger<MessageBrokerHealthCheck> logger)
        {
            _rabbitMqConnection = serviceProvider.GetService<RabbitMqConnection>();
            _kafkaConnection = serviceProvider.GetService<KafkaConnection>();
            _settings = appSettings.Get<MessageBrokerSettings>();
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                switch (_settings.Provider)
                {
                    case MessageBusProvider.RabbitMQ:
                        if (_rabbitMqConnection!=null && _rabbitMqConnection.IsRabbitMqConnected())
                        {
                            return await Task.FromResult(HealthCheckResult.Healthy("RabbitMQ is connected"));
                        }
                        return await Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ is not connected"));

                    case MessageBusProvider.Kafka:
                        if (_kafkaConnection!=null && _kafkaConnection.IsKafkaConnected())
                        {
                            return await Task.FromResult(HealthCheckResult.Healthy("Kafka is connected"));
                        }
                        return await Task.FromResult(HealthCheckResult.Unhealthy("Kafka is not connected"));

                    case MessageBusProvider.InMemory:
                        return await Task.FromResult(HealthCheckResult.Healthy("In-memory message broker is active"));

                    default:
                        return await Task.FromResult(HealthCheckResult.Unhealthy($"Unknown message broker: {_settings.Provider}"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Message broker health check failed");
                return await Task.FromResult(HealthCheckResult.Unhealthy("Message broker health check failed", ex));
            }

        }
    }
}
