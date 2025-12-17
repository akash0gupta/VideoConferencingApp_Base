using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using VideoConferencingApp.Infrastructure.Configuration;
using VideoConferencingApp.Infrastructure.Configuration.Settings; // Assuming this is your settings class

namespace VideoConferencingApp.Infrastructure.Messaging.RabbitMq
{
    public class RabbitMqConnection : IDisposable
    {
        private readonly MessageBrokerSettings _settings;
        private readonly ILogger<RabbitMqConnection> _logger;
        private IConnection _connection;
        private IChannel _channel;
        private readonly object _lock = new object();

        public RabbitMqConnection(AppSettings settings, ILogger<RabbitMqConnection> logger)
        {
            _settings = settings.Get<MessageBrokerSettings>();
            _logger = logger;
        }

        public IChannel Channel
        {
            get
            {
                lock (_lock)
                {
                    if (_channel == null || _channel.IsClosed)
                    {
                        _channel = GetConnection().CreateChannelAsync().GetAwaiter().GetResult();
                        ConfigureExchangeAsync(_channel).GetAwaiter().GetResult();
                    }
                    return _channel;
                }
            }
        }

        public bool IsRabbitMqConnected()
        {
            lock (_lock)
            {
                return _connection != null && _connection.IsOpen;
            }
        }

        private IConnection GetConnection()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.Hostname,
                    Port = _settings.Port,
                    UserName = _settings.Username,
                    Password = _settings.Password,
                    VirtualHost = _settings.VirtualHost
                };

                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _logger.LogInformation("RabbitMQ connection established");
            }

            return _connection;
        }

        private async Task ConfigureExchangeAsync(IChannel channel)
        {
            await channel.ExchangeDeclareAsync(
                exchange: _settings.ExchangeName,
                type: _settings.ExchangeType,
                durable: _settings.ExchangeDurable,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("Exchange '{ExchangeName}' configured '{ExchangeType}'", _settings.ExchangeName, _settings.ExchangeType);
        }

        public void Dispose()
        {
            _channel?.CloseAsync().GetAwaiter().GetResult();
            _channel?.Dispose();
            _connection?.CloseAsync().GetAwaiter().GetResult();
            _connection?.Dispose();
            _logger.LogInformation("RabbitMQ connection closed");
        }
    }


}