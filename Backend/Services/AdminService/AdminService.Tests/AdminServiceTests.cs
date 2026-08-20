using AdminService.Application.DTOs;
using AdminService.Application.Services;
using AdminService.Domain.Entities;
using AdminService.Domain.Interfaces;
using Moq;

namespace AdminService.Tests;

// ══════════════════════════════════════════════════════════════════════
// ADMIN SERVICE — UNIT TESTS
// Covers: Dashboard metrics, User management, Revenue calculation
// ══════════════════════════════════════════════════════════════════════
[TestFixture]
public class AdminDashboardServiceTests
{
    private Mock<IOrderSnapshotRepository> _orderRepo = null!;
    private Mock<IUserSnapshotRepository> _userRepo = null!;
    private AdminDashboardService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _orderRepo = new Mock<IOrderSnapshotRepository>();
        _userRepo  = new Mock<IUserSnapshotRepository>();
        _sut       = new AdminDashboardService(_orderRepo.Object, _userRepo.Object);
    }

    // ── Dashboard Metrics ─────────────────────────────────────────────

    [Test]
    public async Task GetDashboard_NoOrders_ReturnsZeroMetrics()
    {
        _orderRepo.Setup(r => r.GetAllAsync(null, null, null, null))
                  .ReturnsAsync(new List<OrderSnapshot>());
        _orderRepo.Setup(r => r.GetTopRestaurantsAsync(5, null, null))
                  .ReturnsAsync(new List<(Guid, string, int, decimal)>());
        _userRepo.Setup(r => r.GetAllAsync(null, null))
                 .ReturnsAsync(new List<UserSnapshot>());

        var result = await _sut.GetDashboardAsync();

        Assert.That(result.TotalOrders, Is.EqualTo(0));
        Assert.That(result.TotalRevenue, Is.EqualTo(0));
        Assert.That(result.AdminRevenue, Is.EqualTo(0));
        Assert.That(result.TotalUsers, Is.EqualTo(0));
    }

    [Test]
    public async Task GetDashboard_WithDeliveredOrders_CalculatesRevenueCorrectly()
    {
        // Admin revenue = Platform Fee (₹15) + 15% commission per delivered order
        var restaurantId = Guid.NewGuid();
        var orders = new List<OrderSnapshot>
        {
            new() { Id = Guid.NewGuid(), Status = "Delivered", TotalAmount = 400m, RestaurantId = restaurantId, PlacedAt = DateTime.UtcNow.AddHours(-1) },
            new() { Id = Guid.NewGuid(), Status = "Delivered", TotalAmount = 200m, RestaurantId = restaurantId, PlacedAt = DateTime.UtcNow.AddHours(-2) }
        };

        _orderRepo.Setup(r => r.GetAllAsync(null, null, null, null)).ReturnsAsync(orders);
        _orderRepo.Setup(r => r.GetTopRestaurantsAsync(5, null, null))
                  .ReturnsAsync(new List<(Guid, string, int, decimal)>());
        _userRepo.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(new List<UserSnapshot>());

        var result = await _sut.GetDashboardAsync();

        // Order 1: ₹15 + (400 * 0.15) = ₹15 + ₹60 = ₹75
        // Order 2: ₹15 + (200 * 0.15) = ₹15 + ₹30 = ₹45
        // Total admin revenue = ₹120
        Assert.That(result.AdminRevenue, Is.EqualTo(120m));
        Assert.That(result.TotalRevenue, Is.EqualTo(600m)); // 400 + 200
        Assert.That(result.OrdersDelivered, Is.EqualTo(2));
    }

    [Test]
    public async Task GetDashboard_WithRefundRejectedOrders_IncludesCancellationCharge()
    {
        // Admin revenue from RefundRejected = Platform Fee (₹15) + 5% cancellation charge
        var orders = new List<OrderSnapshot>
        {
            new() { Id = Guid.NewGuid(), Status = "RefundRejected", TotalAmount = 300m, RestaurantId = Guid.NewGuid(), PlacedAt = DateTime.UtcNow.AddHours(-1) }
        };

        _orderRepo.Setup(r => r.GetAllAsync(null, null, null, null)).ReturnsAsync(orders);
        _orderRepo.Setup(r => r.GetTopRestaurantsAsync(5, null, null))
                  .ReturnsAsync(new List<(Guid, string, int, decimal)>());
        _userRepo.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(new List<UserSnapshot>());

        var result = await _sut.GetDashboardAsync();

        // ₹15 + (300 * 0.05) = ₹15 + ₹15 = ₹30
        Assert.That(result.AdminRevenue, Is.EqualTo(30m));
    }

    [Test]
    public async Task GetDashboard_TodayOrders_CountedSeparately()
    {
        var today = DateTime.UtcNow.Date;
        var orders = new List<OrderSnapshot>
        {
            new() { Id = Guid.NewGuid(), Status = "Delivered", TotalAmount = 200m, RestaurantId = Guid.NewGuid(), PlacedAt = today.AddHours(10) },
            new() { Id = Guid.NewGuid(), Status = "Delivered", TotalAmount = 300m, RestaurantId = Guid.NewGuid(), PlacedAt = today.AddDays(-1) } // yesterday
        };

        _orderRepo.Setup(r => r.GetAllAsync(null, null, null, null)).ReturnsAsync(orders);
        _orderRepo.Setup(r => r.GetTopRestaurantsAsync(5, null, null))
                  .ReturnsAsync(new List<(Guid, string, int, decimal)>());
        _userRepo.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(new List<UserSnapshot>());

        var result = await _sut.GetDashboardAsync();

        Assert.That(result.TotalOrders, Is.EqualTo(2));
        Assert.That(result.OrdersToday, Is.EqualTo(1));
        Assert.That(result.RevenueToday, Is.EqualTo(200m));
    }

    [Test]
    public async Task GetDashboard_OrderStatusBreakdown_CountsCorrectly()
    {
        var orders = new List<OrderSnapshot>
        {
            new() { Id = Guid.NewGuid(), Status = "Paid",            TotalAmount = 100m, RestaurantId = Guid.NewGuid(), PlacedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), Status = "Delivered",       TotalAmount = 200m, RestaurantId = Guid.NewGuid(), PlacedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), Status = "Cancelled",       TotalAmount = 150m, RestaurantId = Guid.NewGuid(), PlacedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), Status = "Preparing",       TotalAmount = 180m, RestaurantId = Guid.NewGuid(), PlacedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), Status = "OutForDelivery",  TotalAmount = 220m, RestaurantId = Guid.NewGuid(), PlacedAt = DateTime.UtcNow.AddDays(-1) },
        };

        _orderRepo.Setup(r => r.GetAllAsync(null, null, null, null)).ReturnsAsync(orders);
        _orderRepo.Setup(r => r.GetTopRestaurantsAsync(5, null, null))
                  .ReturnsAsync(new List<(Guid, string, int, decimal)>());
        _userRepo.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(new List<UserSnapshot>());

        var result = await _sut.GetDashboardAsync();

        Assert.That(result.TotalOrders, Is.EqualTo(5));
        Assert.That(result.OrdersPaid, Is.EqualTo(1));
        Assert.That(result.OrdersDelivered, Is.EqualTo(1));
        Assert.That(result.OrdersCancelled, Is.EqualTo(1));
        Assert.That(result.OrdersInProgress, Is.EqualTo(2)); // Preparing + OutForDelivery
    }

    [Test]
    public async Task GetDashboard_ActiveDeliveryAgents_CountedCorrectly()
    {
        var users = new List<UserSnapshot>
        {
            new() { Id = Guid.NewGuid(), Role = "DeliveryAgent", IsActive = true },
            new() { Id = Guid.NewGuid(), Role = "DeliveryAgent", IsActive = true },
            new() { Id = Guid.NewGuid(), Role = "DeliveryAgent", IsActive = false }, // inactive
            new() { Id = Guid.NewGuid(), Role = "Customer", IsActive = true },       // not an agent
        };

        _orderRepo.Setup(r => r.GetAllAsync(null, null, null, null)).ReturnsAsync(new List<OrderSnapshot>());
        _orderRepo.Setup(r => r.GetTopRestaurantsAsync(5, null, null))
                  .ReturnsAsync(new List<(Guid, string, int, decimal)>());
        _userRepo.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(users);

        var result = await _sut.GetDashboardAsync();

        Assert.That(result.ActiveDeliveryAgents, Is.EqualTo(2));
        Assert.That(result.TotalUsers, Is.EqualTo(4));
    }

    [Test]
    public async Task GetDashboard_TopRestaurants_MappedCorrectly()
    {
        var restaurantId = Guid.NewGuid();
        var topRestaurants = new List<(Guid RestaurantId, string Name, int Orders, decimal Revenue)>
        {
            (restaurantId, "Best Biryani", 50, 15000m)
        };

        _orderRepo.Setup(r => r.GetAllAsync(null, null, null, null)).ReturnsAsync(new List<OrderSnapshot>());
        _orderRepo.Setup(r => r.GetTopRestaurantsAsync(5, null, null)).ReturnsAsync(topRestaurants);
        _userRepo.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(new List<UserSnapshot>());

        var result = await _sut.GetDashboardAsync();

        Assert.That(result.TopRestaurants.Count, Is.EqualTo(1));
        Assert.That(result.TopRestaurants[0].Name, Is.EqualTo("Best Biryani"));
        Assert.That(result.TopRestaurants[0].OrderCount, Is.EqualTo(50));
        Assert.That(result.TopRestaurants[0].Revenue, Is.EqualTo(15000m));
    }
}

// ══════════════════════════════════════════════════════════════════════
// ADMIN USER SERVICE — UNIT TESTS
// ══════════════════════════════════════════════════════════════════════
[TestFixture]
public class AdminUserServiceTests
{
    private Mock<IUserSnapshotRepository> _userRepo = null!;
    private Mock<IAdminAuditLogRepository> _auditRepo = null!;
    private AdminUserService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _userRepo  = new Mock<IUserSnapshotRepository>();
        _auditRepo = new Mock<IAdminAuditLogRepository>();
        _sut       = new AdminUserService(_userRepo.Object, _auditRepo.Object);
    }

    [Test]
    public async Task GetAllUsers_NoFilter_ReturnsAllUsers()
    {
        var users = new List<UserSnapshot>
        {
            new() { Id = Guid.NewGuid(), FullName = "Alice", Email = "alice@test.com", Role = "Customer", IsActive = true },
            new() { Id = Guid.NewGuid(), FullName = "Bob",   Email = "bob@test.com",   Role = "Partner",  IsActive = true }
        };
        _userRepo.Setup(r => r.GetAllAsync(null, null)).ReturnsAsync(users);

        var result = await _sut.GetAllUsersAsync(null, null);

        Assert.That(result.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetUserById_ExistingUser_ReturnsDto()
    {
        var userId = Guid.NewGuid();
        var user = new UserSnapshot { Id = userId, FullName = "Charlie", Email = "charlie@test.com", Role = "Admin", IsActive = true };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        var result = await _sut.GetUserByIdAsync(userId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FullName, Is.EqualTo("Charlie"));
    }

    [Test]
    public async Task GetUserById_NonExistentUser_ReturnsNull()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((UserSnapshot?)null);

        var result = await _sut.GetUserByIdAsync(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ToggleUserStatus_ExistingUser_UpdatesSnapshotAndLogsAudit()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new UserSnapshot { Id = userId, FullName = "Dave", Email = "dave@test.com", Role = "Customer", IsActive = true };
        _userRepo.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _userRepo.Setup(r => r.SetActiveAsync(userId, false)).Returns(Task.CompletedTask);
        _userRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _auditRepo.Setup(r => r.AddAsync(It.IsAny<AdminAuditLog>())).Returns(Task.CompletedTask);
        _auditRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.ToggleUserStatusAsync(userId, new ToggleUserStatusDto { IsActive = false, Reason = "Violation" }, adminId);

        _userRepo.Verify(r => r.SetActiveAsync(userId, false), Times.Once);
        _auditRepo.Verify(r => r.AddAsync(It.Is<AdminAuditLog>(
            a => a.Action == "DeactivateUser" && a.EntityId == userId)), Times.Once);
    }

    [Test]
    public void ToggleUserStatus_NonExistentUser_ThrowsKeyNotFoundException()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((UserSnapshot?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.ToggleUserStatusAsync(Guid.NewGuid(),
                new ToggleUserStatusDto { IsActive = false, Reason = "Test" },
                Guid.NewGuid()));
    }
}
