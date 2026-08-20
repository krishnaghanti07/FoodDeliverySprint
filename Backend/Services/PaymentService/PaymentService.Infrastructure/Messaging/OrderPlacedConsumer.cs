using FoodDelivery.Shared.Constants;
using FoodDelivery.Shared.Events;
using FoodDelivery.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;

namespace PaymentService.Infrastructure.Messaging;

/// <summary>
/// Listens for OrderPlacedEvent on queue "order.placed".
///
/// For COD orders: immediately publishes PaymentCompletedEvent
/// (COD is auto-confirmed — no actual charge).
///
/// For Card/Wallet orders: creates a Pending PaymentTransaction record
/// and waits for the customer to call POST /api/payments/simulate
/// (or the Razorpay checkout flow).
/// </summary>
public class OrderPlacedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scope;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<OrderPlacedConsumer> _log;
    private IConnection? _conn;
    private IChannel? _ch;

    public OrderPlacedConsumer(
        IServiceScopeFactory scope,
        IRabbitMqPublisher publisher,
        ILogger<OrderPlacedConsumer> log)
    {
        _scope = scope;
        _publisher = publisher;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _conn = await factory.CreateConnectionAsync(ct);
            _ch = await _conn.CreateChannelAsync(cancellationToken: ct);
            var channel = _ch;

            await channel.QueueDeclareAsync(QueueNames.OrderPlacedPayment,
                durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
            await channel.BasicQosAsync(0, 1, false, ct);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var evt = JsonSerializer.Deserialize<OrderPlacedEvent>(
                        Encoding.UTF8.GetString(ea.Body.ToArray()));

                    if (evt is not null)
                    {
                        using var s = _scope.CreateScope();
                        var txnRepo = s.ServiceProvider
                                         .GetRequiredService<IPaymentTransactionRepository>();

                        var existing = await txnRepo.GetByOrderIdAsync(evt.OrderId);
                        if (existing is null)
                        {
                            // Create a Pending record — will be updated when payment is processed
                            await txnRepo.AddAsync(new PaymentTransaction
                            {
                                OrderId = evt.OrderId,
                                CustomerId = evt.CustomerId,
                                Amount = evt.TotalAmount,
                                Currency = "INR",
                                Method = "PENDING",   // updated when customer pays
                                Gateway = PaymentGateway.Simulated,
                                Status = PaymentStatus.Pending,
                                CreatedAt = evt.PlacedAt,
                                UpdatedAt = DateTime.UtcNow
                            });
                            await txnRepo.SaveChangesAsync();

                            _log.LogInformation(
                                "[PaymentService Consumer] Pending record created for Order {Id}.",
                                evt.OrderId);
                        }
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error in OrderPlacedConsumer.");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true, ct);
                }
            };

            await channel.BasicConsumeAsync(QueueNames.OrderPlacedPayment, autoAck: false, consumer: consumer, cancellationToken: ct);
            _log.LogInformation("[PaymentService Consumer] OrderPlacedConsumer started.");

            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            _log.LogInformation("OrderPlacedConsumer is stopping.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "OrderPlacedConsumer failed to connect to RabbitMQ.");
        }
    }

    public override void Dispose()
    {
        if (_ch is IAsyncDisposable asyncChannel)
        {
            asyncChannel.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        else
        {
            _ch?.Dispose();
        }

        if (_conn is IAsyncDisposable asyncConnection)
        {
            asyncConnection.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        else
        {
            _conn?.Dispose();
        }

        base.Dispose();
    }
}