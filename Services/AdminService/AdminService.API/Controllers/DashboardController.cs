using System.Security.Claims;
using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.API.Controllers;

// ══════════════════════════════════════════════════════════════════════
// DASHBOARD CONTROLLER
// PRD: GET /gateway/admin/dashboard
// ══════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IAdminDashboardService _svc;
    public DashboardController(IAdminDashboardService svc) => _svc = svc;

    /// <summary>
    /// Get platform KPIs: total orders, GMV, users, top restaurants, status breakdown.
    /// PRD page 11.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var dash = await _svc.GetDashboardAsync();
        return Ok(ApiResponse<DashboardDto>.Ok(dash, "Dashboard loaded successfully."));
    }
}

// ══════════════════════════════════════════════════════════════════════
// USERS CONTROLLER
// PRD: Admin manages users, partners, delivery agents
// Admin accounts can ONLY be created via seeding (security requirement)
// ══════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _svc;
    public AdminUsersController(IAdminUserService svc) => _svc = svc;

    /// <summary>
    /// List all users. Filter by role (Customer|Partner|Admin|DeliveryAgent)
    /// and/or isActive status.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? role,
        [FromQuery] bool? isActive)
    {
        var users = await _svc.GetAllUsersAsync(role, isActive);
        return Ok(ApiResponse<List<UserSummaryDto>>.Ok(users));
    }

    /// <summary>Get a specific user's profile.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var u = await _svc.GetUserByIdAsync(id);
        if (u is null) return NotFound(ApiResponse<UserSummaryDto>.Fail("User not found."));
        return Ok(ApiResponse<UserSummaryDto>.Ok(u));
    }

    /// <summary>
    /// Activate or deactivate a user account.
    /// Reason is mandatory — logged to audit trail.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ToggleStatus(Guid id, [FromBody] ToggleUserStatusDto dto)
    {
        try
        {
            await _svc.ToggleUserStatusAsync(id, dto, GetAdminId());
            var msg = dto.IsActive ? "User activated." : "User deactivated.";
            return Ok(ApiResponse<string>.Ok(msg));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<string>.Fail(ex.Message)); }
    }

    private Guid GetAdminId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

// ══════════════════════════════════════════════════════════════════════
// ORDERS CONTROLLER
// PRD: GET /gateway/admin/orders  |  PUT /gateway/admin/orders/{id}/status
// ══════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly IAdminOrderService _svc;
    public AdminOrdersController(IAdminOrderService svc) => _svc = svc;

    /// <summary>
    /// Get all orders with optional filters.
    /// Filters: status, restaurantId, from (ISO date), to (ISO date).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] Guid? restaurantId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var orders = await _svc.GetAllOrdersAsync(status, restaurantId, from, to);
        return Ok(ApiResponse<List<AdminOrderDto>>.Ok(orders));
    }

    /// <summary>Get full detail of a single order.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var o = await _svc.GetOrderByIdAsync(id);
        if (o is null) return NotFound(ApiResponse<AdminOrderDto>.Fail("Order not found."));
        return Ok(ApiResponse<AdminOrderDto>.Ok(o));
    }

    /// <summary>
    /// Admin override: update order status.
    /// PRD: Only supported transitions allowed.
    /// Reason is mandatory for Cancelled / RefundInitiated / Refunded.
    /// Every change is logged to the audit trail.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id, [FromBody] AdminUpdateOrderStatusDto dto)
    {
        try
        {
            var order = await _svc.UpdateOrderStatusAsync(id, dto, GetAdminId());
            return Ok(ApiResponse<AdminOrderDto>.Ok(order,
                $"Order status updated to '{dto.NewStatus}'."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<AdminOrderDto>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<AdminOrderDto>.Fail(ex.Message)); }
        catch (ArgumentException ex) { return BadRequest(ApiResponse<AdminOrderDto>.Fail(ex.Message)); }
    }

    private Guid GetAdminId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

// ══════════════════════════════════════════════════════════════════════
// REPORTS CONTROLLER
// PRD: GET /gateway/admin/reports/sales  |  GET /gateway/admin/reports/partners
// ══════════════════════════════════════════════════════════════════════
[ApiController]
[Route("api/admin/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IAdminReportService _svc;
    public ReportsController(IAdminReportService svc) => _svc = svc;

    /// <summary>
    /// Sales report: GMV, order counts, cancellation rate, daily breakdown,
    /// payment method mix. Date range mandatory per PRD.
    /// </summary>
    [HttpGet("sales")]
    public async Task<IActionResult> Sales(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        // PRD: date filters mandatory for bounded reports
        var f = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var t = to ?? DateTime.UtcNow.Date.AddDays(1);

        try
        {
            var report = await _svc.GetSalesReportAsync(f, t);
            return Ok(ApiResponse<SalesReportDto>.Ok(report, "Sales report generated."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<SalesReportDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Partner performance report: orders, revenue, fulfil rate per restaurant.
    /// </summary>
    [HttpGet("partners")]
    public async Task<IActionResult> Partners(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var f = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var t = to ?? DateTime.UtcNow.Date.AddDays(1);

        try
        {
            var report = await _svc.GetPartnerReportAsync(f, t);
            return Ok(ApiResponse<PartnerReportDto>.Ok(report, "Partner report generated."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<PartnerReportDto>.Fail(ex.Message));
        }
    }
}