using System.Security.Claims;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/deliveries")]
[Authorize]
public class DeliveriesController : ControllerBase
{
    private readonly IDeliveryService _deliveryService;
    public DeliveriesController(IDeliveryService deliveryService) =>
        _deliveryService = deliveryService;

    /// <summary>Admin: Assign a delivery agent to a ReadyForPickup order</summary>
    [HttpPost("assign")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Assign([FromBody] AssignDeliveryAgentDto dto)
    {
        try
        {
            var result = await _deliveryService.AssignAgentAsync(dto);
            return Ok(ApiResponse<DeliveryAssignmentDto>.Ok(result, "Agent assigned."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<DeliveryAssignmentDto>.Fail(ex.Message));
        }
    }

    /// <summary>Admin: List all orders ready for pickup with no agent yet</summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPending()
    {
        var list = await _deliveryService.GetPendingUnassignedAsync();
        return Ok(ApiResponse<List<DeliveryAssignmentDto>>.Ok(list));
    }

    /// <summary>Delivery Agent: Get all my assigned deliveries</summary>
    [HttpGet("my")]
    [Authorize(Roles = "DeliveryAgent")]
    public async Task<IActionResult> GetMyDeliveries()
    {
        var list = await _deliveryService.GetMyDeliveriesAsync(GetUserId());
        return Ok(ApiResponse<List<DeliveryAssignmentDto>>.Ok(list));
    }

    /// <summary>Delivery Agent: Get all available orders ready for pickup</summary>
    [HttpGet("available")]
    [Authorize(Roles = "DeliveryAgent")]
    public async Task<IActionResult> GetAvailableOrders()
    {
        var list = await _deliveryService.GetAvailableOrdersAsync();
        return Ok(ApiResponse<List<AvailableOrderDto>>.Ok(list));
    }

    /// <summary>Delivery Agent: Accept an available order</summary>
    [HttpPost("{orderId:guid}/accept")]
    [Authorize(Roles = "DeliveryAgent")]
    public async Task<IActionResult> AcceptOrder(Guid orderId)
    {
        try
        {
            var result = await _deliveryService.AcceptOrderAsync(orderId, GetUserId());
            return Ok(ApiResponse<DeliveryAssignmentDto>.Ok(result, "Order accepted successfully."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<DeliveryAssignmentDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<DeliveryAssignmentDto>.Fail(ex.Message)); }
        catch (Exception ex) { return BadRequest(ApiResponse<DeliveryAssignmentDto>.Fail(ex.Message)); }
    }

    /// <summary>Track delivery for a specific order (Customer/Partner/Admin/Agent)</summary>
    [HttpGet("track/{orderId:guid}")]
    [Authorize(Roles = "Customer,Partner,Admin,DeliveryAgent")]
    public async Task<IActionResult> TrackByOrder(Guid orderId)
    {
        var result = await _deliveryService.GetByOrderIdAsync(orderId);
        if (result is null)
            return NotFound(ApiResponse<DeliveryAssignmentDto>.Fail("No delivery found for this order."));
        return Ok(ApiResponse<DeliveryAssignmentDto>.Ok(result));
    }

    /// <summary>Get delivery assignment detail by ID</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "DeliveryAgent,Admin")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var result = await _deliveryService.GetByIdAsync(id);
        if (result is null)
            return NotFound(ApiResponse<DeliveryAssignmentDto>.Fail("Delivery assignment not found."));
        return Ok(ApiResponse<DeliveryAssignmentDto>.Ok(result));
    }

    /// <summary>
    /// Delivery Agent: Update milestone status.
    /// Assigned → PickedUp → OutForDelivery → Delivered (or Failed).
    /// Also mirrors status onto parent Order.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "DeliveryAgent")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateDeliveryStatusDto dto)
    {
        try
        {
            var result = await _deliveryService.UpdateStatusAsync(id, dto, GetUserId());
            return Ok(ApiResponse<DeliveryAssignmentDto>.Ok(result,
                $"Delivery status updated to {dto.Status}."));
        }
        catch (KeyNotFoundException ex) 
        { 
            return NotFound(ApiResponse<DeliveryAssignmentDto>.Fail(ex.Message)); 
        }
        catch (UnauthorizedAccessException) 
        { 
            return Forbid(); 
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Log the full exception for debugging
            Console.WriteLine($"Concurrency Exception: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            return BadRequest(ApiResponse<DeliveryAssignmentDto>.Fail(
                $"Concurrency error: {ex.Message}. The delivery assignment may have been modified by another process."));
        }
        catch (Exception ex) 
        { 
            // Log the full exception for debugging
            Console.WriteLine($"Exception: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            return BadRequest(ApiResponse<DeliveryAssignmentDto>.Fail(ex.Message)); 
        }
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}