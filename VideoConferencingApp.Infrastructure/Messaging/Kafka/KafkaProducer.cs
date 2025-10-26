using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;
using VideoConferencingApp.Application.Interfaces.Common.IEventHandlerServices;
using VideoConferencingApp.Domain.Events;
using VideoConferencingApp.Infrastructure.Configuration.Settings;

namespace VideoConferencingApp.Infrastructure.Messaging.Kafka
{
    public class KafkaProducer : IMessageProducer, IDisposable
    {
        private readonly KafkaConnection _connection;
        private readonly MessageBrokerSettings _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KafkaProducer> _logger;
        private readonly Dictionary<Type, List<Type>> _eventHandlers = new();
        private readonly List<IConsumer<string, byte[]>> _consumers = new();

        public KafkaProducer(
            KafkaConnection connection,
            IOptions<MessageBrokerSettings> settings,
            IServiceProvider serviceProvider,
            ILogger<KafkaProducer> logger)
        {
            _connection = connection;
            _settings = settings.Value;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : BaseEvent
        {
            try
            {
                var eventName = typeof(TEvent).Name;
                var message = JsonSerializer.Serialize(@event);
                var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);

                var deliveryResult = await _connection.Producer.ProduceAsync(
                    eventName,
                    new Message<string, byte[]>
                    {
                        Key = @event.EventId.ToString(),
                        Value = messageBytes,
                        Timestamp = new Timestamp(DateTime.UtcNow)
                    });

                _logger.LogInformation("Published event {EventName} to topic {Topic} at offset {Offset}",
                    eventName, deliveryResult.Topic, deliveryResult.Offset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing event {EventName}", typeof(TEvent).Name);
                throw;
            }
        }

        public void Subscribe<TEvent, TEventHandler>()
            where TEvent : BaseEvent
            where TEventHandler : IEventHandler<TEvent>
        {
            var eventType = typeof(TEvent);
            var handlerType = typeof(TEventHandler);

            if (!_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType] = new List<Type>();
            }

            _eventHandlers[eventType].Add(handlerType);

            Task.Run(() => SetupSubscriptionAsync<TEvent>(eventType, handlerType));
        }

        private async Task SetupSubscriptionAsync<TEvent>(Type eventType, Type handlerType) where TEvent : BaseEvent
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = string.Join(",", _settings.BootstrapServers),
                GroupId = _settings.GroupId,
                EnableAutoCommit = _settings.EnableAutoCommit,
                SecurityProtocol = Enum.Parse<SecurityProtocol>(_settings.SecurityProtocol),
                SessionTimeoutMs = _settings.SessionTimeoutMs,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            var consumer = new ConsumerBuilder<string, byte[]>(config)
                .SetLogHandler((_, message) => _logger.LogInformation("Kafka Consumer: {Message}", message.Message))
                .SetErrorHandler((_, error) => _logger.LogError("Kafka Error: {Error}", error.Reason))
                .Build();

            var topic = eventType.Name;
            consumer.Subscribe(topic);
            _consumers.Add(consumer);

            await Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume();
                            var message = System.Text.Encoding.UTF8.GetString(consumeResult.Message.Value);
                            var @event = JsonSerializer.Deserialize<TEvent>(message);

                            using var scope = _serviceProvider.CreateScope();
                            foreach (var handlerTypeItem in _eventHandlers[eventType])
                            {
                                var handler = scope.ServiceProvider.GetService(handlerTypeItem) as IEventHandler<TEvent>;
                                if (handler != null && @event!=null)
                                {
                                    await handler.HandleAsync(@event);
                                }
                            }

                            if (!_settings.EnableAutoCommit)
                            {
                                consumer.Commit(consumeResult);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing message from topic {Topic}", topic);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    consumer.Close();
                }
            });
        }

        public void Dispose()
        {
            foreach (var consumer in _consumers)
            {
                consumer.Close();
                consumer.Dispose();
            }
            _connection?.Dispose();
        }
    }
}
