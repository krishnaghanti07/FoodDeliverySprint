using System.Security.Claims;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    public OrdersController(IOrderService orderService) => _orderService = orderService;

    /// <summary>Customer: Place order from current cart (runs Saga)</summary>
    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto)
    {
        try
        {
            var order = await _orderService.PlaceOrderAsync(GetUserId(), dto);
            return CreatedAtAction(nameof(GetById), new { id = order.Id },
                ApiResponse<OrderDto>.Ok(order, "Order placed successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<OrderDto>.Fail(ex.Message));
        }
    }

    /// <summary>Get order by ID (Customer sees own; Partner sees restaurant's; Admin sees all)</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var role = GetRole();
            var order = await _orderService.GetByIdAsync(id, GetUserId(), role);
            return Ok(ApiResponse<OrderDto>.Ok(order));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<OrderDto>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>Customer: Get my order history</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyOrders()
    {
        var orders = await _orderService.GetMyOrdersAsync(GetUserId());
        return Ok(ApiResponse<List<OrderDto>>.Ok(orders));
    }

    /// <summary>Customer: Search my orders with filters</summary>
    [HttpGet("search")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> SearchOrders([FromQuery] OrderSearchDto search)
    {
        var result = await _orderService.SearchOrdersAsync(GetUserId(), search);
        return Ok(ApiResponse<PagedOrdersDto>.Ok(result));
    }

    /// <summary>Partner: Get orders for my restaurant</summary>
    [HttpGet("restaurant/{restaurantId:guid}")]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> GetByRestaurant(Guid restaurantId)
    {
        var orders = await _orderService.GetByRestaurantIdAsync(restaurantId);
        return Ok(ApiResponse<List<OrderDto>>.Ok(orders));
    }

    /// <summary>Internal: Get orders for restaurant (for rating sync) - No auth required</summary>
    [HttpGet("internal/restaurant/{restaurantId:guid}/orders")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByRestaurantInternal(Guid restaurantId)
    {
        var orders = await _orderService.GetByRestaurantIdAsync(restaurantId);
        return Ok(ApiResponse<List<OrderDto>>.Ok(orders));
    }

    /// <summary>Admin: Get all orders</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _orderService.GetAllAsync();
        return Ok(ApiResponse<List<OrderDto>>.Ok(orders));
    }

    /// <summary>Internal: Get all orders for AdminService sync (no auth required)</summary>
    [HttpGet("admin/all")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllInternal()
    {
        var orders = await _orderService.GetAllAsync();
        return Ok(ApiResponse<List<OrderDto>>.Ok(orders));
    }

    /// <summary>Internal: Backfill customer/restaurant names for existing orders</summary>
    [HttpPost("admin/backfill-names")]
    [AllowAnonymous]
    public async Task<IActionResult> BackfillNames()
    {
        var updated = await _orderService.BackfillOrderNamesAsync();
        return Ok(new { message = "Backfill completed", ordersUpdated = updated });
    }

    /// <summary>
    /// Update order status. Role-enforced transitions:
    /// Customer: CancelRequested | Partner: Accepted/Preparing/ReadyForPickup |
    /// DeliveryAgent: (use /api/deliveries endpoint) | Admin: any
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Customer,Partner,Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        try
        {
            var order = await _orderService.UpdateStatusAsync(id, dto, GetRole());
            return Ok(ApiResponse<OrderDto>.Ok(order, $"Order status updated to {dto.NewStatus}."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<OrderDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<OrderDto>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<OrderDto>.Fail(ex.Message));
        }
    }

    /// <summary>Partner: Reject an order with reason</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Partner")]
    public async Task<IActionResult> RejectOrder(Guid id, [FromBody] RejectOrderDto dto)
    {
        try
        {
            var order = await _orderService.RejectOrderAsync(id, dto.RejectionReason, GetUserId());
            return Ok(ApiResponse<OrderDto>.Ok(order, "Order rejected successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<OrderDto>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<OrderDto>.Fail(ex.Message));
        }
    }

    /// <summary>Customer: Soft delete an order (hide from list)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> DeleteOrder(Guid id)
    {
        try
        {
            var result = await _orderService.SoftDeleteOrderAsync(id, GetUserId());
            return Ok(ApiResponse<bool>.Ok(result, "Order deleted successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<bool>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.Fail(ex.Message));
        }
    }

    /// <summary>Customer: Reorder - add items from previous order to cart</summary>
    [HttpPost("{id:guid}/reorder")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Reorder(Guid id)
    {
        try
        {
            var result = await _orderService.ReorderAsync(id, GetUserId());
            return Ok(ApiResponse<ReorderResponseDto>.Ok(result, result.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<ReorderResponseDto>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>Customer: Get my orders with filter (active, completed, rejected)</summary>
    [HttpGet("my/filtered")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetMyOrdersFiltered([FromQuery] string? filter)
    {
        var orders = await _orderService.GetMyOrdersFilteredAsync(GetUserId(), filter);
        return Ok(ApiResponse<List<OrderDto>>.Ok(orders));
    }

    /// <summary>Partner: Get restaurant orders with filter (new, inprogress, completed)</summary>
    [HttpGet("restaurant/{restaurantId:guid}/filtered")]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> GetRestaurantOrdersFiltered(Guid restaurantId, [FromQuery] string? filter)
    {
        var orders = await _orderService.GetRestaurantOrdersFilteredAsync(restaurantId, filter);
        return Ok(ApiResponse<List<OrderDto>>.Ok(orders));
    }

    /// <summary>Customer: Check if order can be cancelled</summary>
    [HttpGet("{id:guid}/can-cancel")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CanCancelOrder(Guid id)
    {
        try
        {
            var order = await _orderService.GetByIdAsync(id, GetUserId(), GetRole());
            var canCancel = order.Status == "Paid" || order.Status == "AwaitingAcceptance" || order.Status == "PaymentPending";
            var reason = canCancel ? null : "Order cannot be cancelled after it has been accepted by the restaurant.";
            
            return Ok(ApiResponse<object>.Ok(new { canCancel, reason }));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>Customer: Cancel an order</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderDto dto)
    {
        try
        {
            var order = await _orderService.CancelOrderAsync(id, GetUserId(), dto.Reason);
            return Ok(ApiResponse<OrderDto>.Ok(order, "Order cancelled successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<OrderDto>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<OrderDto>.Fail(ex.Message));
        }
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetRole() => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
