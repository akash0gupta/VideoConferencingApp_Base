using VideoConferencingApp.Application.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.Configuration.Settings
{
    public enum MessageBusProvider
    {
        InMemory,
        RabbitMQ,
        Kafka
    }
    /// <summary>
    /// Represents the settings for connecting to a message broker like RabbitMQ.
    /// </summary>
    public class MessageBrokerSettings: IConfig
    {
        public  string SectionName => "MessageBroker";

        public MessageBusProvider Provider { get; set; } = MessageBusProvider.InMemory;

        // Kafka Settings
        public string[] BootstrapServers { get; set; } = { "localhost:9092" };
        public string GroupId { get; set; } = "videoconferencing_group";
        public string ClientId { get; set; } = "videoconferencing_client";
        public bool EnableAutoCommit { get; set; } = false;
        public string SecurityProtocol { get; set; } = "PLAINTEXT";
        public int SessionTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// The hostname of the RabbitMQ server.
        /// </summary>
        public string Hostname { get; set; } = "localhost";

        /// <summary>
        /// The port used to connect to RabbitMQ (default: 5672).
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// The username for connecting to RabbitMQ.
        /// </summary>
        public string Username { get; set; } = "guest";

        /// <summary>
        /// The password for connecting to RabbitMQ.
        /// </summary>
        public string Password { get; set; } = "guest";

        /// <summary>
        /// The virtual host in RabbitMQ (default is "/").
        /// </summary>
        public string VirtualHost { get; set; } = "/";

        /// <summary>
        /// The name of the main exchange to publish events to.
        /// </summary>
        public string ExchangeName { get; set; } = "videoconferencing_events";

        /// <summary>
        /// The type of the exchange (default: direct, but could be direct/topic).
        /// </summary>
        public string ExchangeType { get; set; } = "direct";

        /// <summary>
        /// Whether the exchange should be durable.
        /// </summary>
        public bool ExchangeDurable { get; set; } = true;

        /// <summary>
        /// Whether queues should be durable.
        /// </summary>
        public bool QueueDurable { get; set; } = true;

        /// <summary>
        /// Whether messages should be marked as persistent.
        /// </summary>
        public bool PersistentMessages { get; set; } = true;

        /// <summary>
        /// Optional retry queue suffix for failed messages.
        /// </summary>
        public string RetryQueueSuffix { get; set; } = "_retry";

        /// <summary>
        /// Optional dead-letter queue suffix for unprocessed messages.
        /// </summary>
        public string DeadLetterQueueSuffix { get; set; } = "_dlq";

        public QueueSettings Queues { get; set; }
    }

}