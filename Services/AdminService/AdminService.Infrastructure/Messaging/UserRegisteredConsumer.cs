using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;
using FoodDelivery.Shared.Constants;
using FoodDelivery.Shared.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;

namespace AdminService.Infrastructure.Messaging;

// ══════════════════════════════════════════════════════════════════════
// UserRegisteredConsumer
// Listens: user.registered  →  upserts UserSnapshot in AdminDB
// ══════════════════════════════════════════════════════════════════════
public class UserRegisteredConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scope;
    private readonly ILogger<UserRegisteredConsumer> _log;
    private IConnection? _conn;
    private IChannel? _ch;

    public UserRegisteredConsumer(
        IServiceScopeFactory scope,
        ILogger<UserRegisteredConsumer> log)
    { _scope = scope; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _conn = await factory.CreateConnectionAsync(ct);
            _ch = await _conn.CreateChannelAsync(cancellationToken: ct);
            var channel = _ch;

            await channel.QueueDeclareAsync(QueueNames.UserRegistered,
                durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
            await channel.BasicQosAsync(0, 1, false, ct);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var evt = JsonSerializer.Deserialize<UserRegisteredEvent>(
                        Encoding.UTF8.GetString(ea.Body.ToArray()));

                    if (evt is not null)
                    {
                        using var s = _scope.CreateScope();
                        var userRepo = s.ServiceProvider
                                          .GetRequiredService<IUserSnapshotRepository>();
                        await userRepo.UpsertAsync(new UserSnapshot
                        {
                            Id = evt.UserId,
                            FullName = evt.FullName,
                            Email = evt.Email,
                            Mobile = string.Empty,
                            Role = evt.Role,
                            IsActive = true,
                            RegisteredAt = evt.RegisteredAt
                        });
                        await userRepo.SaveChangesAsync();
                        _log.LogInformation(
                            "[Admin Consumer] UserSnapshot upserted: {Id} ({Role}).",
                            evt.UserId, evt.Role);
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error in UserRegisteredConsumer.");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true, ct);
                }
            };

            await channel.BasicConsumeAsync(QueueNames.UserRegistered, autoAck: false, consumer: consumer, cancellationToken: ct);
            _log.LogInformation("[Admin Consumer] UserRegisteredConsumer started.");

            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            _log.LogInformation("UserRegisteredConsumer is stopping.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "UserRegisteredConsumer failed to connect to RabbitMQ.");
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

// ══════════════════════════════════════════════════════════════════════
// OrderPlacedConsumer
// Listens: order.placed  →  creates OrderSnapshot in AdminDB
// ══════════════════════════════════════════════════════════════════════
public class OrderPlacedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scope;
    private readonly ILogger<OrderPlacedConsumer> _log;
    private IConnection? _conn;
    private IChannel? _ch;

    public OrderPlacedConsumer(
        IServiceScopeFactory scope,
        ILogger<OrderPlacedConsumer> log)
    { _scope = scope; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _conn = await factory.CreateConnectionAsync(ct);
            _ch = await _conn.CreateChannelAsync(cancellationToken: ct);
            var channel = _ch;

            await channel.QueueDeclareAsync(QueueNames.OrderPlaced,
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
                        var orderRepo = s.ServiceProvider
                                           .GetRequiredService<IOrderSnapshotRepository>();

                        // Try to look up customer email from UserSnapshot
                        var userRepo = s.ServiceProvider
                                         .GetRequiredService<IUserSnapshotRepository>();
                        var customer = await userRepo.GetByIdAsync(evt.CustomerId);

                        await orderRepo.UpsertAsync(new OrderSnapshot
                        {
                            Id = evt.OrderId,
                            CustomerId = evt.CustomerId,
                            CustomerEmail = customer?.Email ?? string.Empty,
                            RestaurantId = evt.RestaurantId,
                            RestaurantName = string.Empty,   // enriched when status updates
                            TotalAmount = evt.TotalAmount,
                            Status = "PaymentPending",
                            PlacedAt = evt.PlacedAt,
                            UpdatedAt = DateTime.UtcNow
                        });
                        await orderRepo.SaveChangesAsync();
                        _log.LogInformation(
                            "[Admin Consumer] OrderSnapshot created: {Id}.", evt.OrderId);
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error in OrderPlacedConsumer.");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true, ct);
                }
            };

            await channel.BasicConsumeAsync(QueueNames.OrderPlaced, autoAck: false, consumer: consumer, cancellationToken: ct);
            _log.LogInformation("[Admin Consumer] OrderPlacedConsumer started.");

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

// ══════════════════════════════════════════════════════════════════════
// PaymentCompletedConsumer (Admin copy)
// Listens: payment.completed  →  updates OrderSnapshot status to Paid
// ══════════════════════════════════════════════════════════════════════
public class AdminPaymentCompletedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scope;
    private readonly ILogger<AdminPaymentCompletedConsumer> _log;
    private IConnection? _conn;
    private IChannel? _ch;

    public AdminPaymentCompletedConsumer(
        IServiceScopeFactory scope,
        ILogger<AdminPaymentCompletedConsumer> log)
    { _scope = scope; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _conn = await factory.CreateConnectionAsync(ct);
            _ch = await _conn.CreateChannelAsync(cancellationToken: ct);
            var channel = _ch;

            // Use a dedicated queue name so admin gets its own copy
            const string queue = "admin.payment.completed";
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
            await channel.BasicQosAsync(0, 1, false, ct);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var evt = JsonSerializer.Deserialize<PaymentCompletedEvent>(
                        Encoding.UTF8.GetString(ea.Body.ToArray()));

                    if (evt is not null)
                    {
                        using var s = _scope.CreateScope();
                        var orderRepo = s.ServiceProvider
                                         .GetRequiredService<IOrderSnapshotRepository>();
                        await orderRepo.UpdateStatusAsync(evt.OrderId, "Paid", null);
                        await orderRepo.SaveChangesAsync();
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error in AdminPaymentCompletedConsumer.");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true, ct);
                }
            };

            await channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer, cancellationToken: ct);

            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            _log.LogInformation("AdminPaymentCompletedConsumer is stopping.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "AdminPaymentCompletedConsumer failed to start.");
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

// ══════════════════════════════════════════════════════════════════════
// DeliveryStatusUpdatedConsumer (Admin copy)
// Listens: delivery.status_updated → updates OrderSnapshot status
// ══════════════════════════════════════════════════════════════════════
public class AdminDeliveryStatusConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scope;
    private readonly ILogger<AdminDeliveryStatusConsumer> _log;
    private IConnection? _conn;
    private IChannel? _ch;

    public AdminDeliveryStatusConsumer(
        IServiceScopeFactory scope,
        ILogger<AdminDeliveryStatusConsumer> log)
    { _scope = scope; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _conn = await factory.CreateConnectionAsync(ct);
            _ch = await _conn.CreateChannelAsync(cancellationToken: ct);
            var channel = _ch;

            const string queue = "admin.delivery.status_updated";
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
            await channel.BasicQosAsync(0, 1, false, ct);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var evt = JsonSerializer.Deserialize<DeliveryStatusUpdatedEvent>(
                        Encoding.UTF8.GetString(ea.Body.ToArray()));

                    if (evt is not null)
                    {
                        using var s = _scope.CreateScope();
                        var orderRepo = s.ServiceProvider
                                         .GetRequiredService<IOrderSnapshotRepository>();
                        await orderRepo.UpdateStatusAsync(evt.OrderId, evt.NewStatus, null);
                        await orderRepo.SaveChangesAsync();
                    }

                    await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error in AdminDeliveryStatusConsumer.");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true, ct);
                }
            };

            await channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer, cancellationToken: ct);

            await Task.Delay(Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            _log.LogInformation("AdminDeliveryStatusConsumer is stopping.");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "AdminDeliveryStatusConsumer failed to start.");
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