using System.Security.Claims;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.DTOs;
using PaymentService.Application.Interfaces;

namespace PaymentService.API.Controllers;

// ══════════════════════════════════════════════════════════════════════
// SIMULATE PAYMENT CONTROLLER
// PRD page 8: POST /gateway/payments/simulate
// ══════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentSimulationService _simSvc;
    private readonly IPaymentQueryService _querySvc;
    private readonly IRefundService _refundSvc;

    public PaymentsController(
        IPaymentSimulationService simSvc,
        IPaymentQueryService querySvc,
        IRefundService refundSvc)
    {
        _simSvc = simSvc;
        _querySvc = querySvc;
        _refundSvc = refundSvc;
    }

    /// <summary>
    /// Simulate payment success or failure (PRD page 8).
    /// ShouldSucceed=true → publishes PaymentCompletedEvent → OrderService marks order Paid.
    /// ShouldSucceed=false → publishes PaymentFailedEvent → OrderService marks order PaymentFailed.
    /// Customer can retry by calling this again with ShouldSucceed=true.
    ///
    /// Method: COD | Card | Wallet
    /// </summary>
    [HttpPost("simulate")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> Simulate([FromBody] SimulatePaymentDto dto)
    {
        // Auto-fill CustomerId from JWT if not provided
        if (dto.CustomerId == Guid.Empty)
            dto.CustomerId = GetUserId();

        try
        {
            var result = await _simSvc.SimulateAsync(dto);
            var msg = result.Status == "Success"
                ? "Payment successful. Order will be marked as Paid."
                : "Payment failed. You can retry with ShouldSucceed=true.";
            return Ok(ApiResponse<PaymentResultDto>.Ok(result, msg));
        }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<PaymentResultDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<PaymentResultDto>.Fail(ex.Message)); }
    }

    /// <summary>Get payment record by order ID</summary>
    [HttpGet("order/{orderId:guid}")]
    [Authorize(Roles = "Customer,Admin,Partner")]
    public async Task<IActionResult> GetByOrder(Guid orderId)
    {
        var txn = await _querySvc.GetByOrderIdAsync(orderId);
        if (txn is null)
            return NotFound(ApiResponse<PaymentTransactionDto>.Fail(
                "No payment record found for this order."));
        return Ok(ApiResponse<PaymentTransactionDto>.Ok(txn));
    }

    /// <summary>Get payment record by transaction ID</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var txn = await _querySvc.GetByIdAsync(id);
        if (txn is null)
            return NotFound(ApiResponse<PaymentTransactionDto>.Fail("Transaction not found."));
        return Ok(ApiResponse<PaymentTransactionDto>.Ok(txn));
    }

    /// <summary>Customer: Get my payment history</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMine()
    {
        var list = await _querySvc.GetByCustomerIdAsync(GetUserId());
        return Ok(ApiResponse<List<PaymentTransactionDto>>.Ok(list));
    }

    /// <summary>Admin: Get all payment transactions with optional filters</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var list = await _querySvc.GetAllAsync(status, from, to);
        return Ok(ApiResponse<List<PaymentTransactionDto>>.Ok(list));
    }

    /// <summary>
    /// Admin: Process a refund.
    /// PRD: "Refund amount cannot exceed paid amount."
    /// Publishes event to update order status to Refunded.
    /// </summary>
    [HttpPost("refund")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Refund([FromBody] RefundRequestDto dto)
    {
        try
        {
            var result = await _refundSvc.ProcessRefundAsync(dto, GetUserId());
            return Ok(ApiResponse<RefundResultDto>.Ok(result,
                $"Refund of ₹{result.RefundAmount} processed for order {dto.OrderId}."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<RefundResultDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<RefundResultDto>.Fail(ex.Message)); }
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
}

// ══════════════════════════════════════════════════════════════════════
// RAZORPAY CONTROLLER (Optional integration — stub ready)
// ══════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/payments/razorpay")]
public class RazorpayController : ControllerBase
{
    private readonly IRazorpayService _razorpaySvc;

    public RazorpayController(IRazorpayService razorpaySvc) =>
        _razorpaySvc = razorpaySvc;

    /// <summary>
    /// Step 1: Create a Razorpay order before showing payment modal.
    /// Returns razorpay_order_id + key_id for the frontend Razorpay SDK.
    /// Stub in development — calls real API in production.
    /// </summary>
    [HttpPost("create-order")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateRazorpayOrderDto dto)
    {
        try
        {
            var resp = await _razorpaySvc.CreateOrderAsync(dto);
            return Ok(ApiResponse<RazorpayOrderResponseDto>.Ok(resp,
                "Razorpay order created. Use razorpayOrderId in frontend checkout."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<RazorpayOrderResponseDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Create Razorpay order WITHOUT creating database order.
    /// Used for new flow where order is created only after payment succeeds.
    /// </summary>
    [HttpPost("create-order-only")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CreateOrderOnly([FromBody] CreateRazorpayOrderOnlyDto dto)
    {
        try
        {
            var resp = await _razorpaySvc.CreateOrderOnlyAsync(dto);
            return Ok(ApiResponse<RazorpayOrderResponseDto>.Ok(resp,
                "Razorpay order created. Complete payment to place order."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<RazorpayOrderResponseDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Step 2: Verify Razorpay payment after frontend checkout completes.
    /// Verifies HMAC signature and publishes PaymentCompletedEvent.
    /// </summary>
    [HttpPost("verify")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Verify([FromBody] VerifyRazorpayPaymentDto dto)
    {
        try
        {
            var result = await _razorpaySvc.VerifyAndCaptureAsync(dto);
            return Ok(ApiResponse<PaymentResultDto>.Ok(result,
                result.Status == "Success"
                    ? "Payment verified. Order marked as Paid."
                    : "Payment verification failed."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<PaymentResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Handle payment cancellation when user closes Razorpay modal.
    /// Publishes PaymentFailedEvent to trigger saga compensation.
    /// </summary>
    [HttpPost("cancel")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CancelPayment([FromBody] CancelPaymentDto dto)
    {
        try
        {
            var result = await _razorpaySvc.CancelPaymentAsync(dto);
            return Ok(ApiResponse<PaymentResultDto>.Ok(result,
                "Payment cancelled. Order marked as PaymentFailed."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<PaymentResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Razorpay webhook — receives payment.captured / payment.failed events.
    /// Validates X-Razorpay-Signature header. No auth token required (webhook call).
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(
        [FromBody] RazorpayWebhookDto dto,
        [FromHeader(Name = "X-Razorpay-Signature")] string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return BadRequest(ApiResponse<string>.Fail("Missing X-Razorpay-Signature header."));

        try
        {
            await _razorpaySvc.HandleWebhookAsync(dto, signature);
            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }
}