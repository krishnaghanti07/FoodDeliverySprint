using PaymentService.Application.DTOs;

namespace PaymentService.Application.Interfaces;

public interface IPaymentSimulationService
{
    /// <summary>
    /// Simulate success/failure — primary testing endpoint.
    /// Publishes PaymentCompletedEvent or PaymentFailedEvent to RabbitMQ.
    /// </summary>
    Task<PaymentResultDto> SimulateAsync(SimulatePaymentDto dto);
}

public interface IRazorpayService
{
    /// <summary>
    /// Step 1 — Create Razorpay order (returns order_id for frontend checkout).
    /// Stub: generates a fake order_id; in production calls Razorpay REST API.
    /// </summary>
    Task<RazorpayOrderResponseDto> CreateOrderAsync(CreateRazorpayOrderDto dto);

    /// <summary>
    /// Create Razorpay order WITHOUT database order (new flow).
    /// Returns razorpay_order_id for payment, order created only after payment succeeds.
    /// </summary>
    Task<RazorpayOrderResponseDto> CreateOrderOnlyAsync(CreateRazorpayOrderOnlyDto dto);

    /// <summary>
    /// Step 2 — Verify Razorpay payment signature after frontend checkout.
    /// Stub: validates format; in production verifies HMAC-SHA256 signature.
    /// Publishes PaymentCompletedEvent on success.
    /// </summary>
    Task<PaymentResultDto> VerifyAndCaptureAsync(VerifyRazorpayPaymentDto dto);

    /// <summary>
    /// Handle payment cancellation when user closes Razorpay modal.
    /// Publishes PaymentFailedEvent to trigger saga compensation.
    /// </summary>
    Task<PaymentResultDto> CancelPaymentAsync(CancelPaymentDto dto);

    /// <summary>
    /// Handle Razorpay webhook events (payment.captured / payment.failed).
    /// Validates X-Razorpay-Signature header before processing.
    /// </summary>
    Task HandleWebhookAsync(RazorpayWebhookDto dto, string signature);
}

public interface IRefundService
{
    /// <summary>
    /// Initiate a refund for a paid order.
    /// PRD: "Refund amount cannot exceed paid amount."
    /// Publishes PaymentFailedEvent (reuse for refund notification).
    /// </summary>
    Task<RefundResultDto> ProcessRefundAsync(RefundRequestDto dto, Guid adminId);
}

public interface IPaymentQueryService
{
    Task<PaymentTransactionDto?> GetByOrderIdAsync(Guid orderId);
    Task<PaymentTransactionDto?> GetByIdAsync(Guid id);
    Task<List<PaymentTransactionDto>> GetByCustomerIdAsync(Guid customerId);
    Task<List<PaymentTransactionDto>> GetAllAsync(string? status, DateTime? from, DateTime? to);
}