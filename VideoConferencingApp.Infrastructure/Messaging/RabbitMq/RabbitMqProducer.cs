using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Events;
using VideoConferencingApp.Infrastructure.Configuration.Settings;
using VideoConferencingApp.Domain.Events;
using VideoConferencingApp.Application.Interfaces.Common.IEventHandlerServices;
using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;

namespace VideoConferencingApp.Infrastructure.Messaging.RabbitMq
{
    public class RabbitMqProducer : IMessageProducer, IDisposable
    {
        private readonly RabbitMqConnection _connection;
        private readonly MessageBrokerSettings _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMqProducer> _logger;
        private readonly Dictionary<Type, List<Type>> _eventHandlers = new Dictionary<Type, List<Type>>();
        private readonly List<AsyncEventingBasicConsumer> _consumers = new List<AsyncEventingBasicConsumer>();

        public RabbitMqProducer(
            RabbitMqConnection connection,
            IOptions<MessageBrokerSettings> settings,
            IServiceProvider serviceProvider,
            ILogger<RabbitMqProducer> logger)
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
                var body = Encoding.UTF8.GetBytes(message);

                var properties = new BasicProperties
                {
                    Persistent = _settings.PersistentMessages,
                    Type = eventName,
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                };

                await _connection.Channel.BasicPublishAsync(
                    exchange: _settings.ExchangeName,
                    routingKey: eventName,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation("Published event {EventName} to exchange {ExchangeName}",
                    eventName, _settings.ExchangeName);
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

            Task.Run(async () => await SetupSubscriptionAsync<TEvent>(eventType, handlerType));
        }

        private async Task SetupSubscriptionAsync<TEvent>(Type eventType, Type handlerType) where TEvent : BaseEvent
        {
            var queueName = $"{eventType.Name}_queue";
            var retryQueueName = $"{queueName}{_settings.RetryQueueSuffix}";
            var dlqName = $"{queueName}{_settings.DeadLetterQueueSuffix}";

            // Declare dead letter queue
            await _connection.Channel.QueueDeclareAsync(
                queue: dlqName,
                durable: _settings.QueueDurable,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Declare main queue with dead letter exchange
            var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "" },
            { "x-dead-letter-routing-key", dlqName }
        };

            await _connection.Channel.QueueDeclareAsync(
                queue: queueName,
                durable: _settings.QueueDurable,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            // Bind queue to exchange
            await _connection.Channel.QueueBindAsync(
                queue: queueName,
                exchange: _settings.ExchangeName,
                routingKey: eventType.Name);

            // Create consumer
            var consumer = new AsyncEventingBasicConsumer(_connection.Channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var @event = JsonSerializer.Deserialize<TEvent>(message);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        foreach (var handlerTypeItem in _eventHandlers[eventType])
                        {
                            var handler = scope.ServiceProvider.GetService(handlerTypeItem) as IEventHandler<TEvent>;
                            if (handler != null && @event!=null)
                            {
                                await handler.HandleAsync(@event);
                            }
                        }
                    }

                    await _connection.Channel.BasicAckAsync(ea.DeliveryTag, false);
                    _logger.LogInformation("Successfully processed event {EventName}", eventType.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event {EventName}", eventType.Name);
                    await _connection.Channel.BasicNackAsync(ea.DeliveryTag, false, false);
                }
            };

            await _connection.Channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            _consumers.Add(consumer);
            _logger.LogInformation("Subscribed to event {EventName} with handler {HandlerName}",
                eventType.Name, handlerType.Name);
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
