using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VideoConferencingApp.Infrastructure.Configuration.Settings;

namespace VideoConferencingApp.Infrastructure.Messaging.Kafka
{
    public class KafkaConnection : IDisposable
    {
        private readonly MessageBrokerSettings _settings;
        private readonly ILogger<KafkaConnection> _logger;
        private IProducer<string, byte[]> _producer;
        private readonly object _lock = new();

        public KafkaConnection(
            IOptions<MessageBrokerSettings> settings,
            ILogger<KafkaConnection> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public IProducer<string, byte[]> Producer
        {
            get
            {
                lock (_lock)
                {
                    if (_producer == null)
                    {
                        var config = new ProducerConfig
                        {
                            BootstrapServers = string.Join(",", _settings.BootstrapServers),
                            ClientId = _settings.ClientId,
                            SecurityProtocol = Enum.Parse<SecurityProtocol>(_settings.SecurityProtocol),
                            EnableIdempotence = true,
                            MessageSendMaxRetries = 3,
                        };

                        _producer = new ProducerBuilder<string, byte[]>(config)
                            .SetLogHandler((_, message) =>
                                _logger.LogInformation("Kafka Producer: {Message}", message.Message))
                            .SetErrorHandler((_, error) =>
                                _logger.LogError("Kafka Error: {Error}", error.Reason))
                            .Build();

                        _logger.LogInformation("Kafka producer created");
                    }
                    return _producer;
                }
            }
        }

        public bool IsKafkaConnected()
        {
            lock (_lock)
            {
                return _producer != null;
            }
        }

        public void Dispose()
        {
            _producer?.Dispose();
            _logger.LogInformation("Kafka producer disposed");
        }
    }
}
