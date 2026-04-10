using System.Security.Cryptography;
using System.Text;
using FoodDelivery.Shared.Constants;
using FoodDelivery.Shared.Events;
using FoodDelivery.Shared.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Interfaces;

namespace PaymentService.Application.Services;

// ══════════════════════════════════════════════════════════════════════
// PAYMENT SIMULATION SERVICE
// PRD page 8: "Simulate payment success and failure"
// Publishes PaymentCompletedEvent or PaymentFailedEvent to RabbitMQ
// ══════════════════════════════════════════════════════════════════════
public class PaymentSimulationService : IPaymentSimulationService
{
    private readonly IPaymentTransactionRepository _repo;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<PaymentSimulationService> _log;

    public PaymentSimulationService(
        IPaymentTransactionRepository repo,
        IRabbitMqPublisher publisher,
        ILogger<PaymentSimulationService> log)
    {
        _repo = repo;
        _publisher = publisher;
        _log = log;
    }

    public async Task<PaymentResultDto> SimulateAsync(SimulatePaymentDto dto)
    {
        // Validate method
        var validMethods = new[] { "COD", "CARD", "WALLET" };
        var method = dto.Method.ToUpperInvariant().Trim();
        if (!validMethods.Contains(method))
            throw new ArgumentException(
                $"Invalid payment method '{dto.Method}'. Allowed: COD, Card, Wallet.");

        if (dto.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        // Check for duplicate payment attempt on same order
        var existing = await _repo.GetByOrderIdAsync(dto.OrderId);
        if (existing is not null && existing.Status == PaymentStatus.Success)
            throw new InvalidOperationException(
                $"Order {dto.OrderId} already has a successful payment (TxnId: {existing.Id}).");

        var now = DateTime.UtcNow;

        var txn = new PaymentTransaction
        {
            OrderId = dto.OrderId,
            CustomerId = dto.CustomerId,
            Amount = dto.Amount,
            Currency = "INR",
            Method = method,
            Gateway = method == "COD" ? PaymentGateway.COD : PaymentGateway.Simulated,
            Status = dto.ShouldSucceed ? PaymentStatus.Success : PaymentStatus.Failed,
            GatewayTxnId = dto.ShouldSucceed
                ? $"SIM_{Guid.NewGuid():N}".ToUpperInvariant()[..20]
                : null,
            FailureReason = dto.ShouldSucceed
                ? null
                : "Simulated payment failure — card declined / insufficient funds.",
            PaidAt = dto.ShouldSucceed ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repo.AddAsync(txn);
        await _repo.SaveChangesAsync();

        // Publish to RabbitMQ — OrderService consumers update order status
        if (dto.ShouldSucceed)
        {
            _publisher.Publish(new PaymentCompletedEvent
            {
                OrderId = dto.OrderId,
                PaymentId = txn.Id,
                AmountPaid = txn.Amount,
                PaymentMethod = txn.Method,
                PaidAt = now
            }, QueueNames.PaymentCompleted);

            _log.LogInformation(
                "[PaymentService] Payment SUCCESS — OrderId={OrderId}, TxnId={TxnId}, Amount=₹{Amount}",
                dto.OrderId, txn.Id, dto.Amount);
        }
        else
        {
            _publisher.Publish(new PaymentFailedEvent
            {
                OrderId = dto.OrderId,
                Reason = txn.FailureReason!,
                FailedAt = now
            }, QueueNames.PaymentFailed);

            _log.LogWarning(
                "[PaymentService] Payment FAILED — OrderId={OrderId}, Reason={Reason}",
                dto.OrderId, txn.FailureReason);
        }

        return new PaymentResultDto
        {
            TransactionId = txn.Id,
            OrderId = txn.OrderId,
            Amount = txn.Amount,
            Method = txn.Method,
            Status = txn.Status.ToString(),
            GatewayTxnId = txn.GatewayTxnId,
            FailureReason = txn.FailureReason,
            ProcessedAt = now
        };
    }
}

// ══════════════════════════════════════════════════════════════════════
// RAZORPAY SERVICE (Stub — ready for production integration)
// Optional topic from user requirements: "Payment Gateways (Razorpay/Stripe)"
// ══════════════════════════════════════════════════════════════════════
public class RazorpayService : IRazorpayService
{
    private readonly IPaymentTransactionRepository _txnRepo;
    private readonly IRazorpayOrderRepository _razorpayRepo;
    private readonly IRabbitMqPublisher _publisher;
    private readonly IConfiguration _config;
    private readonly ILogger<RazorpayService> _log;

    // Razorpay config keys (set in appsettings.json)
    private string KeyId => _config["Razorpay:KeyId"] ?? "rzp_test_STUB_KEY_ID";
    private string KeySecret => _config["Razorpay:KeySecret"] ?? "rzp_test_STUB_SECRET";
    private string WebhookSecret => _config["Razorpay:WebhookSecret"] ?? "razorpay_webhook_secret";

    public RazorpayService(
        IPaymentTransactionRepository txnRepo,
        IRazorpayOrderRepository razorpayRepo,
        IRabbitMqPublisher publisher,
        IConfiguration config,
        ILogger<RazorpayService> log)
    {
        _txnRepo = txnRepo;
        _razorpayRepo = razorpayRepo;
        _publisher = publisher;
        _config = config;
        _log = log;
    }

    public async Task<RazorpayOrderResponseDto> CreateOrderAsync(CreateRazorpayOrderDto dto)
    {
        // In production: call https://api.razorpay.com/v1/orders via HttpClient
        // Stub: generate a fake order_id in Razorpay format
        var razorpayOrderId = $"order_{GenerateAlphanumeric(14)}";

        var razorpayOrder = new RazorpayOrder
        {
            OrderId = dto.OrderId,
            RazorpayOrderId = razorpayOrderId,
            Amount = dto.Amount,
            Currency = "INR",
            Status = "created"
        };

        await _razorpayRepo.AddAsync(razorpayOrder);
        await _razorpayRepo.SaveChangesAsync();

        _log.LogInformation(
            "[Razorpay Stub] Order created: {RzpOrderId} for OrderId={OrderId}",
            razorpayOrderId, dto.OrderId);

        return new RazorpayOrderResponseDto
        {
            RazorpayOrderId = razorpayOrderId,
            Amount = dto.Amount,
            Currency = "INR",
            Key = KeyId    // returned to frontend for Razorpay checkout
        };
    }

    public async Task<PaymentResultDto> VerifyAndCaptureAsync(VerifyRazorpayPaymentDto dto)
    {
        // In production: verify HMAC-SHA256 signature
        // Signature = HMAC_SHA256(razorpay_order_id + "|" + razorpay_payment_id, key_secret)
        var expectedSig = ComputeHmacSha256(
            $"{dto.RazorpayOrderId}|{dto.RazorpayPaymentId}", KeySecret);

        var razorpayOrder = await _razorpayRepo.GetByOrderIdAsync(dto.OrderId)
            ?? throw new KeyNotFoundException("Razorpay order not found for this order.");

        // Stub mode: accept any non-empty signature; in production compare expectedSig
        var signatureValid = !string.IsNullOrWhiteSpace(dto.RazorpaySignature);

        var now = DateTime.UtcNow;
        var txn = new PaymentTransaction
        {
            OrderId = dto.OrderId,
            CustomerId = Guid.Empty, // enriched from OrderService in production
            Amount = razorpayOrder.Amount,
            Currency = "INR",
            Method = "CARD",
            Gateway = PaymentGateway.Razorpay,
            GatewayTxnId = dto.RazorpayPaymentId,
            GatewayOrderId = dto.RazorpayOrderId,
            Status = signatureValid ? PaymentStatus.Success : PaymentStatus.Failed,
            FailureReason = signatureValid ? null : "Invalid Razorpay signature.",
            PaidAt = signatureValid ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _txnRepo.AddAsync(txn);

        // Update Razorpay order status
        razorpayOrder.Status = signatureValid ? "paid" : "failed";
        await _razorpayRepo.UpdateAsync(razorpayOrder);
        await _razorpayRepo.SaveChangesAsync();

        if (signatureValid)
        {
            _publisher.Publish(new PaymentCompletedEvent
            {
                OrderId = dto.OrderId,
                PaymentId = txn.Id,
                AmountPaid = txn.Amount,
                PaymentMethod = "Razorpay",
                PaidAt = now
            }, QueueNames.PaymentCompleted);
        }
        else
        {
            _publisher.Publish(new PaymentFailedEvent
            {
                OrderId = dto.OrderId,
                Reason = "Invalid payment signature.",
                FailedAt = now
            }, QueueNames.PaymentFailed);
        }

        return new PaymentResultDto
        {
            TransactionId = txn.Id,
            OrderId = dto.OrderId,
            Amount = txn.Amount,
            Method = "Razorpay",
            Status = txn.Status.ToString(),
            GatewayTxnId = txn.GatewayTxnId,
            FailureReason = txn.FailureReason,
            ProcessedAt = now
        };
    }

    public async Task HandleWebhookAsync(RazorpayWebhookDto dto, string signature)
    {
        // In production: verify webhook signature before processing
        _log.LogInformation("[Razorpay Webhook] Event: {Event}", dto.Event);

        var paymentItem = dto.Payload?.Payment?.Entity;
        if (paymentItem is null) return;

        var txn = await _txnRepo.GetByOrderIdAsync(Guid.Empty); // look up by GatewayTxnId in prod
        if (txn is null) return;

        var now = DateTime.UtcNow;

        if (dto.Event == "payment.captured")
        {
            txn.Status = PaymentStatus.Success;
            txn.PaidAt = now;
            txn.UpdatedAt = now;
            await _txnRepo.UpdateAsync(txn);
            await _txnRepo.SaveChangesAsync();

            _publisher.Publish(new PaymentCompletedEvent
            {
                OrderId = txn.OrderId,
                PaymentId = txn.Id,
                AmountPaid = txn.Amount,
                PaymentMethod = "Razorpay",
                PaidAt = now
            }, QueueNames.PaymentCompleted);
        }
        else if (dto.Event == "payment.failed")
        {
            txn.Status = PaymentStatus.Failed;
            txn.FailureReason = paymentItem.ErrorDescription ?? "Payment failed via webhook.";
            txn.UpdatedAt = now;
            await _txnRepo.UpdateAsync(txn);
            await _txnRepo.SaveChangesAsync();

            _publisher.Publish(new PaymentFailedEvent
            {
                OrderId = txn.OrderId,
                Reason = txn.FailureReason,
                FailedAt = now
            }, QueueNames.PaymentFailed);
        }
    }

    private static string ComputeHmacSha256(string data, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    private static string GenerateAlphanumeric(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
            .ToArray());
    }
}

// ══════════════════════════════════════════════════════════════════════
// REFUND SERVICE
// PRD: "Refund amount cannot exceed paid amount" (Admin action)
// ══════════════════════════════════════════════════════════════════════
public class RefundService : IRefundService
{
    private readonly IPaymentTransactionRepository _repo;
    private readonly IRabbitMqPublisher _publisher;
    private readonly ILogger<RefundService> _log;

    public RefundService(
        IPaymentTransactionRepository repo,
        IRabbitMqPublisher publisher,
        ILogger<RefundService> log)
    {
        _repo = repo;
        _publisher = publisher;
        _log = log;
    }

    public async Task<RefundResultDto> ProcessRefundAsync(RefundRequestDto dto, Guid adminId)
    {
        var txn = await _repo.GetByOrderIdAsync(dto.OrderId)
            ?? throw new KeyNotFoundException(
                $"No payment record found for order {dto.OrderId}.");

        if (txn.Status != PaymentStatus.Success)
            throw new InvalidOperationException(
                $"Cannot refund. Payment status is '{txn.Status}'. Only successful payments can be refunded.");

        // PRD: "Refund amount cannot exceed paid amount"
        if (dto.RefundAmount > txn.Amount)
            throw new InvalidOperationException(
                $"Refund amount ₹{dto.RefundAmount} exceeds paid amount ₹{txn.Amount}.");

        var now = DateTime.UtcNow;
        txn.Status = dto.RefundAmount == txn.Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartialRefund;
        txn.RefundAmount = dto.RefundAmount;
        txn.RefundReason = dto.Reason;
        txn.RefundedAt = now;
        txn.UpdatedAt = now;

        await _repo.UpdateAsync(txn);
        await _repo.SaveChangesAsync();

        // Notify OrderService so order status moves to Refunded
        _publisher.Publish(new PaymentFailedEvent
        {
            OrderId = dto.OrderId,
            Reason = $"Refund processed: {dto.Reason}",
            FailedAt = now
        }, QueueNames.PaymentFailed);

        _log.LogInformation(
            "[PaymentService] Refund processed — OrderId={OrderId}, Amount=₹{Amount}, AdminId={AdminId}",
            dto.OrderId, dto.RefundAmount, adminId);

        return new RefundResultDto
        {
            TransactionId = txn.Id,
            OrderId = dto.OrderId,
            RefundAmount = dto.RefundAmount,
            Status = txn.Status.ToString(),
            Reason = dto.Reason,
            RefundedAt = now
        };
    }
}

// ══════════════════════════════════════════════════════════════════════
// PAYMENT QUERY SERVICE
// ══════════════════════════════════════════════════════════════════════
public class PaymentQueryService : IPaymentQueryService
{
    private readonly IPaymentTransactionRepository _repo;

    public PaymentQueryService(IPaymentTransactionRepository repo) => _repo = repo;

    public async Task<PaymentTransactionDto?> GetByOrderIdAsync(Guid orderId)
    {
        var t = await _repo.GetByOrderIdAsync(orderId);
        return t is null ? null : MapDto(t);
    }

    public async Task<PaymentTransactionDto?> GetByIdAsync(Guid id)
    {
        var t = await _repo.GetByIdAsync(id);
        return t is null ? null : MapDto(t);
    }

    public async Task<List<PaymentTransactionDto>> GetByCustomerIdAsync(Guid customerId)
    {
        var list = await _repo.GetByCustomerIdAsync(customerId);
        return list.Select(MapDto).ToList();
    }

    public async Task<List<PaymentTransactionDto>> GetAllAsync(
        string? status, DateTime? from, DateTime? to)
    {
        var list = await _repo.GetAllAsync(status, from, to);
        return list.Select(MapDto).ToList();
    }

    private static PaymentTransactionDto MapDto(PaymentTransaction t) => new()
    {
        Id = t.Id,
        OrderId = t.OrderId,
        CustomerId = t.CustomerId,
        Amount = t.Amount,
        Currency = t.Currency,
        Method = t.Method,
        Status = t.Status.ToString(),
        Gateway = t.Gateway.ToString(),
        GatewayTxnId = t.GatewayTxnId,
        FailureReason = t.FailureReason,
        RefundReason = t.RefundReason,
        RefundAmount = t.RefundAmount,
        CreatedAt = t.CreatedAt,
        PaidAt = t.PaidAt,
        RefundedAt = t.RefundedAt
    };
}