namespace PaymentService.Domain.Entities;

// ══════════════════════════════════════════════════════════════════════
// PaymentService is the dedicated payment microservice.
//
// Responsibilities:
//   • Receive OrderPlacedEvent from OrderService via RabbitMQ
//   • Process payments (simulated + Razorpay/Stripe stub)
//   • Publish PaymentCompletedEvent / PaymentFailedEvent back
//   • Maintain its own payment transaction ledger (PaymentDB)
//   • Expose payment history and refund endpoints
//
// The /gateway/orders/payments/simulate endpoint in OrderService
// calls this service internally (or both co-exist during the stub phase).
// This service owns the canonical payment record.
// ══════════════════════════════════════════════════════════════════════

public class PaymentTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Method { get; set; } = string.Empty; // COD | Card | Wallet | Razorpay | Stripe
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentGateway Gateway { get; set; } = PaymentGateway.Simulated;
    public string? GatewayTxnId { get; set; } // Razorpay/Stripe transaction ID
    public string? GatewayOrderId { get; set; } // Razorpay order_id / Stripe PaymentIntent
    public string? FailureReason { get; set; }
    public string? RefundReason { get; set; }
    public decimal? RefundAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}

public enum PaymentStatus
{
    Pending = 0,
    Success = 1,
    Failed = 2,
    Refunded = 3,
    PartialRefund = 4
}

public enum PaymentGateway
{
    Simulated = 0,
    Razorpay = 1,
    Stripe = 2,
    COD = 3
}

/// <summary>
/// Razorpay order stub — stores the Razorpay order_id before payment capture.
/// In production: created via Razorpay API, returned to frontend for checkout.
/// </summary>
public class RazorpayOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }   // FoodDelivery OrderId
    public string RazorpayOrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Status { get; set; } = "created"; // created | paid | failed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}