using System.Security.Claims;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;

namespace OrderService.API.Controllers;

[ApiController]
[Route("api/orders/coupons")]
[Authorize]
public class CouponsController : ControllerBase
{
    private readonly ICouponService _couponService;
    public CouponsController(ICouponService couponService) => _couponService = couponService;

    /// <summary>Admin/Partner: Create a new coupon</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Partner")]
    public async Task<IActionResult> CreateCoupon([FromBody] CreateCouponDto dto)
    {
        try
        {
            var coupon = await _couponService.CreateCouponAsync(dto, GetUserId(), GetRole());
            return CreatedAtAction(nameof(GetById), new { id = coupon.Id },
                ApiResponse<CouponDto>.Ok(coupon, "Coupon created successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CouponDto>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CouponDto>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>Get coupon by ID</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Partner")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var coupon = await _couponService.GetByIdAsync(id);
            return Ok(ApiResponse<CouponDto>.Ok(coupon));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<CouponDto>.Fail(ex.Message));
        }
    }

    /// <summary>Admin: Get all coupons</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var coupons = await _couponService.GetAllCouponsAsync();
        return Ok(ApiResponse<List<CouponDto>>.Ok(coupons));
    }

    /// <summary>Partner: Get my restaurant coupons</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Partner")]
    public async Task<IActionResult> GetMyCoupons([FromQuery] Guid restaurantId)
    {
        var coupons = await _couponService.GetMyCouponsAsync(restaurantId);
        return Ok(ApiResponse<List<CouponDto>>.Ok(coupons));
    }

    /// <summary>Get active coupons (public)</summary>
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive()
    {
        var coupons = await _couponService.GetActiveCouponsAsync();
        return Ok(ApiResponse<List<CouponDto>>.Ok(coupons));
    }

    /// <summary>Admin/Partner: Update coupon</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Partner")]
    public async Task<IActionResult> UpdateCoupon(Guid id, [FromBody] UpdateCouponDto dto)
    {
        try
        {
            var coupon = await _couponService.UpdateCouponAsync(id, dto, GetUserId(), GetRole());
            return Ok(ApiResponse<CouponDto>.Ok(coupon, "Coupon updated successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<CouponDto>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>Admin/Partner: Delete coupon</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Partner")]
    public async Task<IActionResult> DeleteCoupon(Guid id)
    {
        try
        {
            await _couponService.DeleteCouponAsync(id, GetUserId(), GetRole());
            return Ok(ApiResponse<string>.Ok("Coupon deleted successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>Validate coupon code (public)</summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponDto dto)
    {
        var result = await _couponService.ValidateCouponAsync(dto);
        return Ok(ApiResponse<CouponValidationResultDto>.Ok(result));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string GetRole() => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
