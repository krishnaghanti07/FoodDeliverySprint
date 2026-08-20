using FoodDelivery.Shared.Constants;
using FoodDelivery.Shared.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;

namespace OrderService.Infrastructure.Messaging;

/// <summary>
/// Listens for PaymentFailedEvent and marks the order as PaymentFailed.
/// </summary>
public class PaymentFailedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentFailedConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public PaymentFailedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentFailedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(QueueNames.PaymentFailedOrder,
                durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var @event = JsonSerializer.Deserialize<PaymentFailedEvent>(json);
                if (@event is null)
                {
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var order = await orderRepo.GetByIdAsync(@event.OrderId);

                if (order is not null)
                {
                    // Saga Compensation: Payment failed, cancel the order
                    order.Status = OrderStatus.PaymentFailed;
                    order.CancellationReason = $"Payment Failed: {@event.Reason}";
                    order.UpdatedAt = DateTime.UtcNow;
                    await orderRepo.UpdateAsync(order);
                    await orderRepo.SaveChangesAsync();
                    _logger.LogWarning(
                        "[SAGA COMPENSATION] Order {OrderId} marked as PaymentFailed. Reason: {Reason}", 
                        @event.OrderId, @event.Reason);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            };

            await _channel.BasicConsumeAsync(QueueNames.PaymentFailedOrder, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("PaymentFailedConsumer is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PaymentFailedConsumer failed to start.");
        }
    }

    public override void Dispose()
    {
        if (_channel is IAsyncDisposable asyncChannel)
        {
            asyncChannel.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        else
        {
            _channel?.Dispose();
        }

        if (_connection is IAsyncDisposable asyncConnection)
        {
            asyncConnection.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        else
        {
            _connection?.Dispose();
        }

        base.Dispose();
    }
}