using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/refunds")]
public class RefundController : ControllerBase
{
    private readonly IRefundService _refundService;
    private readonly ILogger<RefundController> _logger;

    public RefundController(IRefundService refundService, ILogger<RefundController> logger)
    {
        _refundService = refundService;
        _logger = logger;
    }

    /// <summary>Get all pending refund requests</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingRefunds()
    {
        try
        {
            var refunds = await _refundService.GetPendingRefundsAsync();
            return Ok(refunds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending refunds");
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }

    /// <summary>Get all refund requests with optional status filter</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllRefunds([FromQuery] string? status = null)
    {
        try
        {
            var refunds = await _refundService.GetAllRefundsAsync(status);
            return Ok(refunds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching refunds");
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }

    /// <summary>Get refund request by ID</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRefundById(Guid id)
    {
        try
        {
            var refund = await _refundService.GetRefundByIdAsync(id);
            
            if (refund == null)
                return NotFound(ApiResponse<object>.Fail("Refund request not found."));
            
            return Ok(refund);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching refund {RefundId}", id);
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }

    /// <summary>Get refund request by order ID</summary>
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetRefundByOrderId(Guid orderId)
    {
        try
        {
            var refund = await _refundService.GetRefundByOrderIdAsync(orderId);
            
            if (refund == null)
                return NotFound(ApiResponse<object>.Fail("No refund request found for this order."));
            
            return Ok(refund);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching refund for order {OrderId}", orderId);
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }

    /// <summary>Process refund request (approve or reject)</summary>
    [HttpPost("{id}/process")]
    public async Task<IActionResult> ProcessRefund(Guid id, [FromBody] ProcessRefundDto dto)
    {
        try
        {
            var processedBy = dto.ProcessedBy ?? Guid.Empty;
            var refund = await _refundService.ProcessRefundAsync(id, dto.Action, dto.AdminNotes, processedBy);
            
            var message = dto.Action.ToLower() == "approve" 
                ? "Refund approved successfully." 
                : "Refund rejected successfully.";
            
            return Ok(ApiResponse<RefundRequestDto>.Ok(refund, message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund {RefundId}", id);
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }

    /// <summary>Approve refund for cancelled paid order</summary>
    [HttpPost("approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveRefund([FromBody] ApproveRefundDto dto)
    {
        try
        {
            _logger.LogInformation("Admin approving refund for order {OrderId}", dto.OrderId);
            
            // Create or get existing refund request
            var refund = await _refundService.ApproveRefundForOrderAsync(
                dto.OrderId,
                dto.CustomerId,
                dto.OriginalAmount,
                dto.PlatformFee,
                dto.CancellationCharge,
                dto.RefundAmount,
                dto.AdminNotes
            );
            
            return Ok(ApiResponse<RefundRequestDto>.Ok(refund, "Refund approved and amount credited to customer wallet."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving refund for order {OrderId}", dto.OrderId);
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }

    /// <summary>Reject refund for cancelled paid order</summary>
    [HttpPost("reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RejectRefund([FromBody] RejectRefundDto dto)
    {
        try
        {
            _logger.LogInformation("Admin rejecting refund for order {OrderId}", dto.OrderId);
            
            await _refundService.RejectRefundForOrderAsync(dto.OrderId, dto.AdminNotes);
            
            return Ok(ApiResponse<object>.Ok(null, "Refund rejected successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting refund for order {OrderId}", dto.OrderId);
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }
}
