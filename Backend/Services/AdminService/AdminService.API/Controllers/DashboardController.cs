using System.Security.Claims;
using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using AdminService.Domain.Interfaces;
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

    /// <summary>Full sync: pull all orders from OrderService + fix names from snapshots</summary>
    [HttpPost("fix-customer-names")]
    [AllowAnonymous]
    public async Task<IActionResult> FixCustomerNames()
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var orderRepo = scope.ServiceProvider.GetRequiredService<IOrderSnapshotRepository>();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserSnapshotRepository>();
        var restaurantRepo = scope.ServiceProvider.GetRequiredService<IRestaurantSnapshotRepository>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        int newOrdersSynced = 0;
        int statusUpdated = 0;

        // ── Step 1: Pull ALL orders from OrderService and upsert into snapshots ──
        try
        {
            var orderServiceUrl = config["Services:OrderService"] ?? "http://localhost:5003";
            var httpClient = httpClientFactory.CreateClient();
            var ordersRes = await httpClient.GetAsync($"{orderServiceUrl}/api/orders/admin/all");

            if (ordersRes.IsSuccessStatusCode)
            {
                var json = await ordersRes.Content.ReadAsStringAsync();
                var parsed = System.Text.Json.JsonDocument.Parse(json);
                var dataEl = parsed.RootElement.TryGetProperty("data", out var d) ? d : parsed.RootElement;

                if (dataEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var o in dataEl.EnumerateArray())
                    {
                        var oid = o.TryGetProperty("id", out var idEl) ? idEl.GetGuid() : Guid.Empty;
                        if (oid == Guid.Empty) continue;

                        var existing = await orderRepo.GetByIdAsync(oid);
                        var status = o.TryGetProperty("status", out var stEl) ? stEl.GetString() ?? "PaymentPending" : "PaymentPending";
                        var customerId = o.TryGetProperty("customerId", out var cidEl) ? cidEl.GetGuid() : Guid.Empty;
                        var restaurantId = o.TryGetProperty("restaurantId", out var ridEl) ? ridEl.GetGuid() : Guid.Empty;
                        var restaurantName = o.TryGetProperty("restaurantName", out var rnEl) ? rnEl.GetString() ?? "" : "";
                        var totalAmount = o.TryGetProperty("totalAmount", out var taEl) ? taEl.GetDecimal() : 0m;
                        var paymentMethod = o.TryGetProperty("paymentMethod", out var pmEl) ? pmEl.GetString() ?? "" : "";
                        var createdAt = o.TryGetProperty("createdAt", out var caEl) ? caEl.GetDateTime() : DateTime.UtcNow;

                        // Look up customer name from UserSnapshot
                        var customer = customerId != Guid.Empty ? await userRepo.GetByIdAsync(customerId) : null;
                        var customerName = customer?.FullName ?? string.Empty;
                        var customerEmail = customer?.Email ?? string.Empty;

                        // Look up restaurant name from RestaurantSnapshot if not in order
                        if (string.IsNullOrEmpty(restaurantName) && restaurantId != Guid.Empty)
                        {
                            var restaurant = await restaurantRepo.GetByIdAsync(restaurantId);
                            restaurantName = restaurant?.Name ?? string.Empty;
                        }

                        if (existing == null)
                        {
                            await orderRepo.UpsertAsync(new AdminService.Domain.Entities.OrderSnapshot
                            {
                                Id = oid,
                                CustomerId = customerId,
                                CustomerEmail = customerEmail,
                                CustomerName = customerName,
                                RestaurantId = restaurantId,
                                RestaurantName = restaurantName,
                                TotalAmount = totalAmount,
                                PaymentMethod = paymentMethod,
                                Status = status,
                                PlacedAt = createdAt,
                                UpdatedAt = DateTime.UtcNow
                            });
                            newOrdersSynced++;
                        }
                        else
                        {
                            // Update status and names if stale
                            bool changed = false;
                            if (existing.Status != status) { existing.Status = status; changed = true; statusUpdated++; }
                            if (string.IsNullOrEmpty(existing.CustomerName) && !string.IsNullOrEmpty(customerName)) { existing.CustomerName = customerName; changed = true; }
                            if (string.IsNullOrEmpty(existing.RestaurantName) && !string.IsNullOrEmpty(restaurantName)) { existing.RestaurantName = restaurantName; changed = true; }
                            if (changed) await orderRepo.UpsertAsync(existing);
                        }
                    }
                    await orderRepo.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Sync] Failed to sync orders from OrderService: {ex.Message}");
        }

        // ── Step 2: Sync RestaurantSnapshots from CatalogService ──
        int restaurantFixed = 0;
        try
        {
            var catalogUrl = config["Services:CatalogService"] ?? "http://localhost:5002";
            var httpClient = httpClientFactory.CreateClient();
            var catalogRes = await httpClient.GetAsync($"{catalogUrl}/api/catalog/restaurants");

            if (catalogRes.IsSuccessStatusCode)
            {
                var json = await catalogRes.Content.ReadAsStringAsync();
                var parsed = System.Text.Json.JsonDocument.Parse(json);
                var dataEl = parsed.RootElement.TryGetProperty("data", out var d) ? d : parsed.RootElement;

                if (dataEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var r in dataEl.EnumerateArray())
                    {
                        var rid = r.TryGetProperty("id", out var idEl) ? idEl.GetGuid() : Guid.Empty;
                        var rname = r.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
                        if (rid == Guid.Empty || string.IsNullOrEmpty(rname)) continue;

                        await restaurantRepo.UpsertAsync(new AdminService.Domain.Entities.RestaurantSnapshot
                        {
                            Id = rid,
                            Name = rname,
                            Description = r.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? "" : "",
                            Address = r.TryGetProperty("address", out var addrEl) ? addrEl.GetString() ?? "" : "",
                            Phone = r.TryGetProperty("phone", out var phoneEl) ? phoneEl.GetString() ?? "" : "",
                            PartnerId = r.TryGetProperty("ownerId", out var ownerEl) ? (ownerEl.ValueKind == System.Text.Json.JsonValueKind.String ? ownerEl.GetGuid() : Guid.Empty) : Guid.Empty,
                            PartnerName = r.TryGetProperty("ownerName", out var ownerNameEl) ? ownerNameEl.GetString() ?? "" : "",
                            Status = r.TryGetProperty("isApproved", out var approvedEl) && approvedEl.GetBoolean() ? "Approved" : "Pending",
                            IsOpen = r.TryGetProperty("isOpen", out var openEl) && openEl.GetBoolean(),
                            AverageRating = r.TryGetProperty("rating", out var ratingEl) ? (decimal)ratingEl.GetDouble() : 0,
                        });
                        restaurantFixed++;
                    }
                    await restaurantRepo.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Sync] Failed to sync restaurants: {ex.Message}");
        }

        var totalAfterSync = (await orderRepo.GetAllAsync(null, null, null, null)).Count;

        return Ok(new
        {
            message = "Full sync completed",
            totalOrdersInSnapshot = totalAfterSync,
            newOrdersSynced,
            statusUpdated,
            restaurantsSynced = restaurantFixed
        });
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