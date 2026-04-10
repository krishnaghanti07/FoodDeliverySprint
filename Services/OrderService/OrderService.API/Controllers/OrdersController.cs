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

    /// <summary>Partner: Get orders for my restaurant</summary>
    [HttpGet("restaurant/{restaurantId:guid}")]
    [Authorize(Roles = "Partner,Admin")]
    public async Task<IActionResult> GetByRestaurant(Guid restaurantId)
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

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetRole() => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}