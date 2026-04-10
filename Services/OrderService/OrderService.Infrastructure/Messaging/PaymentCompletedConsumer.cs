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
/// Listens for PaymentCompletedEvent published by PaymentService/SimulatePayment.
/// Updates order status to Paid.
/// </summary>
public class PaymentCompletedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentCompletedConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public PaymentCompletedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentCompletedConsumer> logger)
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

            await _channel.QueueDeclareAsync(QueueNames.PaymentCompleted,
                durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var @event = JsonSerializer.Deserialize<PaymentCompletedEvent>(json);
                if (@event is null)
                {
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var order = await orderRepo.GetByIdAsync(@event.OrderId);

                if (order is not null && order.Status == OrderStatus.PaymentPending)
                {
                    order.Status = OrderStatus.Paid;
                    order.UpdatedAt = DateTime.UtcNow;
                    await orderRepo.UpdateAsync(order);
                    await orderRepo.SaveChangesAsync();
                    _logger.LogInformation("Order {OrderId} marked Paid via PaymentCompleted event.", @event.OrderId);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            };

            await _channel.BasicConsumeAsync(QueueNames.PaymentCompleted, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("PaymentCompletedConsumer is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PaymentCompletedConsumer failed to start.");
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
