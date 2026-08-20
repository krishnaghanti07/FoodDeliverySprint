using System;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace FoodDelivery.Shared.Messaging;

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly object _publishLock = new();
    private bool _disposed;

    public RabbitMqPublisher(string hostName = "localhost")
    {
        var factory = new ConnectionFactory { HostName = hostName };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public void Publish<T>(T message, string queueName) where T : class
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(queueName))
        {
            throw new ArgumentException("Queue name cannot be null, empty, or whitespace.", nameof(queueName));
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RabbitMqPublisher));
        }

        lock (_publishLock)
        {
            _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false,
                autoDelete: false, arguments: null).GetAwaiter().GetResult();

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var props = new BasicProperties { Persistent = true };

            _channel.BasicPublishAsync(exchange: "", routingKey: queueName,
                mandatory: false, basicProperties: props, body: body).GetAwaiter().GetResult();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_publishLock)
        {
            if (_disposed)
            {
                return;
            }

            if (_channel is IAsyncDisposable asyncChannel)
            {
                asyncChannel.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            else
            {
                _channel.Dispose();
            }

            if (_connection is IAsyncDisposable asyncConnection)
            {
                asyncConnection.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            else
            {
                _connection.Dispose();
            }

            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
