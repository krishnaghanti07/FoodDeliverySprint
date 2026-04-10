using System.ComponentModel.DataAnnotations;

namespace PaymentService.Application.DTOs;

// ── Core Payment DTOs ─────────────────────────────────────────────────

public class PaymentTransactionDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Gateway { get; set; } = string.Empty;
    public string? GatewayTxnId { get; set; }
    public string? FailureReason { get; set; }
    public string? RefundReason { get; set; }
    public decimal? RefundAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}

// ── Simulate Payment ──────────────────────────────────────────────────

/// <summary>
/// PRD: POST /gateway/payments/simulate
/// Used for testing — simulates success or failure of any payment method.
/// </summary>
public class SimulatePaymentDto
{
    [Required] public Guid OrderId { get; set; }
    [Required] public Guid CustomerId { get; set; }
    [Required, Range(0.01, 9999999)] public decimal Amount { get; set; }
    /// <summary>COD | Card | Wallet</summary>
    [Required] public string Method { get; set; } = string.Empty;
    /// <summary>true = payment succeeds, false = payment fails</summary>
    public bool ShouldSucceed { get; set; } = true;
}

public class PaymentResultDto
{
    public Guid TransactionId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;  // Success | Failed
    public string? GatewayTxnId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime ProcessedAt { get; set; }
}

// ── Razorpay Integration (Optional — stub) ────────────────────────────

/// <summary>
/// Create a Razorpay order before showing the payment UI.
/// In production: call Razorpay API, store order_id, return to frontend.
/// </summary>
public class CreateRazorpayOrderDto
{
    [Required] public Guid OrderId { get; set; }
    [Required] public Guid CustomerId { get; set; }
    [Required, Range(1, 9999999)] public decimal Amount { get; set; }
}

public class RazorpayOrderResponseDto
{
    public string RazorpayOrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string Key { get; set; } = string.Empty; // Razorpay Key ID
}

/// <summary>
/// Frontend calls this after Razorpay payment modal completes.
/// Verifies signature and marks payment as success/failure.
/// </summary>
public class VerifyRazorpayPaymentDto
{
    [Required] public Guid OrderId { get; set; }
    [Required] public string RazorpayOrderId { get; set; } = string.Empty;
    [Required] public string RazorpayPaymentId { get; set; } = string.Empty;
    [Required] public string RazorpaySignature { get; set; } = string.Empty;
}

// ── Refund ────────────────────────────────────────────────────────────

public class RefundRequestDto
{
    [Required] public Guid OrderId { get; set; }
    [Required, Range(0.01, 9999999)] public decimal RefundAmount { get; set; }
    [Required, MinLength(5)] public string Reason { get; set; } = string.Empty;
}

public class RefundResultDto
{
    public Guid TransactionId { get; set; }
    public Guid OrderId { get; set; }
    public decimal RefundAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime RefundedAt { get; set; }
}

// ── Webhook ───────────────────────────────────────────────────────────

/// <summary>
/// Razorpay sends webhook events to POST /api/payments/webhooks/razorpay
/// when payment is captured/failed on their end.
/// </summary>
public class RazorpayWebhookDto
{
    public string Event { get; set; } = string.Empty;
    public RazorpayWebhookPayload? Payload { get; set; }
}

public class RazorpayWebhookPayload
{
    public RazorpayPaymentEntity? Payment { get; set; }
}

public class RazorpayPaymentEntity
{
    public RazorpayPaymentItem? Entity { get; set; }
}

public class RazorpayPaymentItem
{
    public string Id { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // captured | failed
    public long Amount { get; set; }  // paise
    public string? ErrorCode { get; set; }
    public string? ErrorDescription { get; set; }
}