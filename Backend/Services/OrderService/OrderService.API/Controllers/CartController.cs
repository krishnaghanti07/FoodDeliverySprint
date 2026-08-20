using System.Security.Claims;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/orders/cart")]
[Authorize(Roles = "Customer")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    public CartController(ICartService cartService) => _cartService = cartService;

    /// <summary>Get my current cart</summary>
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var cart = await _cartService.GetCartAsync(GetUserId());
        return Ok(ApiResponse<CartDto>.Ok(cart));
    }

    /// <summary>Get checkout summary (pricing breakdown)</summary>
    [HttpGet("checkout-context")]
    public async Task<IActionResult> GetCheckoutContext()
    {
        try
        {
            var ctx = await _cartService.GetCheckoutContextAsync(GetUserId());
            return Ok(ApiResponse<CheckoutContextDto>.Ok(ctx));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<CheckoutContextDto>.Fail(ex.Message));
        }
    }

    /// <summary>Add an item to cart</summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemDto dto)
    {
        try
        {
            var cart = await _cartService.AddItemAsync(GetUserId(), dto);
            return Ok(ApiResponse<CartDto>.Ok(cart, "Item added to cart."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CartDto>.Fail(ex.Message));
        }
    }

    /// <summary>Update quantity of a cart item (set 0 to remove)</summary>
    [HttpPut("items/{cartItemId:guid}")]
    public async Task<IActionResult> UpdateItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto)
    {
        try
        {
            var cart = await _cartService.UpdateItemAsync(GetUserId(), cartItemId, dto);
            return Ok(ApiResponse<CartDto>.Ok(cart, "Cart updated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<CartDto>.Fail(ex.Message));
        }
    }

    /// <summary>Remove a specific item from cart</summary>
    [HttpDelete("items/{cartItemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid cartItemId)
    {
        try
        {
            var cart = await _cartService.RemoveItemAsync(GetUserId(), cartItemId);
            return Ok(ApiResponse<CartDto>.Ok(cart, "Item removed."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<CartDto>.Fail(ex.Message));
        }
    }

    /// <summary>Apply coupon code (WELCOME10 | FLAT50 | SAVE20)</summary>
    [HttpPost("apply-coupon")]
    public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponDto dto)
    {
        try
        {
            var cart = await _cartService.ApplyCouponAsync(GetUserId(), dto);
            return Ok(ApiResponse<CartDto>.Ok(cart, "Coupon applied."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<CartDto>.Fail(ex.Message));
        }
    }

    /// <summary>Remove applied coupon</summary>
    [HttpDelete("remove-coupon")]
    public async Task<IActionResult> RemoveCoupon()
    {
        try
        {
            var cart = await _cartService.RemoveCouponAsync(GetUserId());
            return Ok(ApiResponse<CartDto>.Ok(cart, "Coupon removed."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<CartDto>.Fail(ex.Message));
        }
    }

    /// <summary>Clear entire cart</summary>
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        await _cartService.ClearCartAsync(GetUserId());
        return Ok(ApiResponse<string>.Ok("Cart cleared."));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}