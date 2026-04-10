using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/orders/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    public PaymentsController(IPaymentService paymentService) =>
        _paymentService = paymentService;

    /// <summary>
    /// Simulate payment success or failure.
    /// Set ShouldSucceed=true for success, false for failure.
    /// Publishes PaymentCompletedEvent or PaymentFailedEvent to RabbitMQ.
    /// </summary>
    [HttpPost("simulate")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> Simulate([FromBody] SimulatePaymentDto dto)
    {
        try
        {
            var result = await _paymentService.SimulatePaymentAsync(dto);
            var msg = result.Status == "Success" ? "Payment successful." : "Payment failed.";
            return Ok(ApiResponse<PaymentResultDto>.Ok(result, msg));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<PaymentResultDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PaymentResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>Get payment status for an order</summary>
    [HttpGet("order/{orderId:guid}")]
    [Authorize(Roles = "Customer,Admin")]
    public async Task<IActionResult> GetByOrder(Guid orderId)
    {
        var payment = await _paymentService.GetPaymentByOrderIdAsync(orderId);
        if (payment is null)
            return NotFound(ApiResponse<PaymentSummaryDto>.Fail("No payment found for this order."));
        return Ok(ApiResponse<PaymentSummaryDto>.Ok(payment));
    }
}