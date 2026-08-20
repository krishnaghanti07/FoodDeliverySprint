using CatalogService.Application.DTOs;
using CatalogService.Application.Services;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Interfaces;
using Moq;

namespace CatalogService.Tests;

// ══════════════════════════════════════════════════════════════════════
// CATALOG SERVICE — UNIT TESTS
// Covers: Restaurant CRUD, Menu Items, Approval, Soft Delete, Reviews
// ══════════════════════════════════════════════════════════════════════
[TestFixture]
public class CatalogServiceTests
{
    private Mock<IRestaurantRepository> _restaurantRepo = null!;
    private Mock<IMenuItemRepository> _menuItemRepo = null!;
    private Mock<ICategoryRepository> _categoryRepo = null!;
    private Mock<IOperatingHourRepository> _operatingHourRepo = null!;
    private Mock<IReviewRepository> _reviewRepo = null!;
    private CatalogAppService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _restaurantRepo = new Mock<IRestaurantRepository>();
        _menuItemRepo = new Mock<IMenuItemRepository>();
        _categoryRepo = new Mock<ICategoryRepository>();
        _operatingHourRepo = new Mock<IOperatingHourRepository>();
        _reviewRepo = new Mock<IReviewRepository>();

        _sut = new CatalogAppService(
            _restaurantRepo.Object,
            _menuItemRepo.Object,
            _categoryRepo.Object,
            _operatingHourRepo.Object,
            _reviewRepo.Object);
    }

    // ── Create Restaurant ─────────────────────────────────────────────

    [Test]
    public async Task CreateRestaurant_NewPartner_CreatesRestaurant()
    {
        var partnerId = Guid.NewGuid();
        _restaurantRepo.Setup(r => r.GetByPartnerIdAsync(partnerId)).ReturnsAsync((Restaurant?)null);
        _restaurantRepo.Setup(r => r.GetDeletedByPartnerIdAsync(partnerId)).ReturnsAsync((Restaurant?)null);
        _restaurantRepo.Setup(r => r.AddAsync(It.IsAny<Restaurant>())).Returns(Task.CompletedTask);
        _restaurantRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        Restaurant? capturedRestaurant = null;
        _restaurantRepo.Setup(r => r.AddAsync(It.IsAny<Restaurant>()))
                       .Callback<Restaurant>(r => capturedRestaurant = r)
                       .Returns(Task.CompletedTask);

        var dto = new CreateRestaurantDto
        {
            Name = "Pizza Palace", Cuisine = "Italian",
            Address = "123 Main St", City = "Mumbai",
            Phone = "9876543210", PrepTimeMinutes = 30,
            MinOrderAmount = 100m, DeliveryFee = 40m
        };

        var result = await _sut.CreateRestaurantAsync(dto, partnerId);

        Assert.That(capturedRestaurant, Is.Not.Null);
        Assert.That(capturedRestaurant!.Name, Is.EqualTo("Pizza Palace"));
        Assert.That(capturedRestaurant.IsApproved, Is.False, "New restaurants must be unapproved");
    }

    [Test]
    public void CreateRestaurant_PartnerAlreadyHasRestaurant_ThrowsInvalidOperation()
    {
        var partnerId = Guid.NewGuid();
        var existingRestaurant = new Restaurant { Id = Guid.NewGuid(), PartnerUserId = partnerId, Name = "Existing" };
        _restaurantRepo.Setup(r => r.GetByPartnerIdAsync(partnerId)).ReturnsAsync(existingRestaurant);

        var dto = new CreateRestaurantDto { Name = "Second Restaurant", Cuisine = "Chinese", City = "Delhi" };

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateRestaurantAsync(dto, partnerId));
    }

    [Test]
    public void CreateRestaurant_PartnerHasDeletedRestaurant_ThrowsInvalidOperation()
    {
        var partnerId = Guid.NewGuid();
        var deletedRestaurant = new Restaurant { Id = Guid.NewGuid(), PartnerUserId = partnerId, Name = "Deleted", IsDeleted = true };
        _restaurantRepo.Setup(r => r.GetByPartnerIdAsync(partnerId)).ReturnsAsync((Restaurant?)null);
        _restaurantRepo.Setup(r => r.GetDeletedByPartnerIdAsync(partnerId)).ReturnsAsync(deletedRestaurant);

        var dto = new CreateRestaurantDto { Name = "New Restaurant", Cuisine = "Mexican", City = "Pune" };

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateRestaurantAsync(dto, partnerId));
    }

    // ── Approve Restaurant ────────────────────────────────────────────

    [Test]
    public async Task ApproveRestaurant_PendingRestaurant_SetsApprovedTrue()
    {
        var restaurantId = Guid.NewGuid();
        var restaurant = new Restaurant { Id = restaurantId, Name = "Test", IsApproved = false };
        _restaurantRepo.Setup(r => r.GetByIdAsync(restaurantId)).ReturnsAsync(restaurant);
        _restaurantRepo.Setup(r => r.UpdateAsync(It.IsAny<Restaurant>())).Returns(Task.CompletedTask);
        _restaurantRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.ApproveRestaurantAsync(restaurantId);

        Assert.That(restaurant.IsApproved, Is.True);
    }

    [Test]
    public void ApproveRestaurant_NonExistent_ThrowsKeyNotFoundException()
    {
        _restaurantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Restaurant?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.ApproveRestaurantAsync(Guid.NewGuid()));
    }

    // ── Toggle Open Status ────────────────────────────────────────────

    [Test]
    public async Task ToggleOpenStatus_OpenRestaurant_ClosesIt()
    {
        var restaurantId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var restaurant = new Restaurant { Id = restaurantId, PartnerUserId = partnerId, IsOpen = true };
        _restaurantRepo.Setup(r => r.GetByIdAsync(restaurantId)).ReturnsAsync(restaurant);
        _restaurantRepo.Setup(r => r.UpdateAsync(It.IsAny<Restaurant>())).Returns(Task.CompletedTask);
        _restaurantRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.ToggleOpenStatusAsync(restaurantId, partnerId);

        Assert.That(result, Is.False);
        Assert.That(restaurant.IsOpen, Is.False);
    }

    [Test]
    public void ToggleOpenStatus_WrongPartner_ThrowsUnauthorized()
    {
        var restaurantId = Guid.NewGuid();
        var restaurant = new Restaurant { Id = restaurantId, PartnerUserId = Guid.NewGuid() };
        _restaurantRepo.Setup(r => r.GetByIdAsync(restaurantId)).ReturnsAsync(restaurant);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ToggleOpenStatusAsync(restaurantId, Guid.NewGuid()));
    }

    // ── Soft Delete Restaurant ────────────────────────────────────────

    [Test]
    public async Task DeleteRestaurant_PartnerOwned_SoftDeletes()
    {
        var restaurantId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var restaurant = new Restaurant { Id = restaurantId, PartnerUserId = partnerId, IsDeleted = false };
        _restaurantRepo.Setup(r => r.GetByIdAsync(restaurantId)).ReturnsAsync(restaurant);
        _restaurantRepo.Setup(r => r.UpdateAsync(It.IsAny<Restaurant>())).Returns(Task.CompletedTask);
        _restaurantRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.DeleteRestaurantAsync(restaurantId, partnerId, "Partner");

        Assert.That(restaurant.IsDeleted, Is.True);
        Assert.That(restaurant.DeletedAt, Is.Not.Null);
    }

    [Test]
    public void DeleteRestaurant_WrongPartner_ThrowsUnauthorized()
    {
        var restaurantId = Guid.NewGuid();
        var restaurant = new Restaurant { Id = restaurantId, PartnerUserId = Guid.NewGuid() };
        _restaurantRepo.Setup(r => r.GetByIdAsync(restaurantId)).ReturnsAsync(restaurant);

        Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.DeleteRestaurantAsync(restaurantId, Guid.NewGuid(), "Partner"));
    }

    // ── Restore Restaurant ────────────────────────────────────────────

    [Test]
    public async Task RestoreRestaurant_DeletedRestaurant_ClearsDeletedFlags()
    {
        var restaurantId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var restaurant = new Restaurant { Id = restaurantId, PartnerUserId = partnerId, IsDeleted = true };
        _restaurantRepo.Setup(r => r.GetByIdAsync(restaurantId)).ReturnsAsync(restaurant);
        _restaurantRepo.Setup(r => r.GetByPartnerIdAsync(partnerId)).ReturnsAsync((Restaurant?)null);
        _restaurantRepo.Setup(r => r.UpdateAsync(It.IsAny<Restaurant>())).Returns(Task.CompletedTask);
        _restaurantRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.RestoreRestaurantAsync(restaurantId, Guid.NewGuid(), "Admin restore");

        Assert.That(restaurant.IsDeleted, Is.False);
        Assert.That(restaurant.DeletedAt, Is.Null);
    }

    [Test]
    public void RestoreRestaurant_NotDeleted_ThrowsInvalidOperation()
    {
        var restaurant = new Restaurant { Id = Guid.NewGuid(), IsDeleted = false };
        _restaurantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(restaurant);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RestoreRestaurantAsync(Guid.NewGuid(), Guid.NewGuid(), "Reason"));
    }

    [Test]
    public void RestoreRestaurant_PartnerAlreadyHasActiveRestaurant_ThrowsInvalidOperation()
    {
        var restaurantId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var deletedRestaurant = new Restaurant { Id = restaurantId, PartnerUserId = partnerId, IsDeleted = true };
        var activeRestaurant = new Restaurant { Id = Guid.NewGuid(), PartnerUserId = partnerId, IsDeleted = false };
        _restaurantRepo.Setup(r => r.GetByIdAsync(restaurantId)).ReturnsAsync(deletedRestaurant);
        _restaurantRepo.Setup(r => r.GetByPartnerIdAsync(partnerId)).ReturnsAsync(activeRestaurant);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RestoreRestaurantAsync(restaurantId, Guid.NewGuid(), "Reason"));
    }

    // ── Menu Items ────────────────────────────────────────────────────

    [Test]
    public async Task AddMenuItem_ValidData_CreatesMenuItem()
    {
        var categoryId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();
        _menuItemRepo.Setup(r => r.AddAsync(It.IsAny<MenuItem>())).Returns(Task.CompletedTask);
        _menuItemRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        MenuItem? capturedItem = null;
        _menuItemRepo.Setup(r => r.AddAsync(It.IsAny<MenuItem>()))
                     .Callback<MenuItem>(m => capturedItem = m)
                     .Returns(Task.CompletedTask);

        var dto = new CreateMenuItemDto
        {
            Name = "Margherita Pizza", Description = "Classic", Price = 250m,
            IsVeg = true, CategoryId = categoryId, RestaurantId = restaurantId
        };

        var result = await _sut.AddMenuItemAsync(dto);

        Assert.That(capturedItem, Is.Not.Null);
        Assert.That(capturedItem!.Name, Is.EqualTo("Margherita Pizza"));
        Assert.That(capturedItem.Price, Is.EqualTo(250m));
    }

    [Test]
    public async Task ToggleMenuItemAvailability_AvailableItem_MakesUnavailable()
    {
        var itemId = Guid.NewGuid();
        var item = new MenuItem { Id = itemId, Name = "Pasta", IsAvailable = true };
        _menuItemRepo.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);
        _menuItemRepo.Setup(r => r.UpdateAsync(It.IsAny<MenuItem>())).Returns(Task.CompletedTask);
        _menuItemRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.ToggleMenuItemAvailabilityAsync(itemId);

        Assert.That(result, Is.False);
        Assert.That(item.IsAvailable, Is.False);
    }

    // ── Reviews ───────────────────────────────────────────────────────

    [Test]
    public async Task AddReview_NewReview_CreatesReview()
    {
        var restaurantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _reviewRepo.Setup(r => r.GetByUserAndRestaurantAsync(userId, restaurantId)).ReturnsAsync((Review?)null);
        _reviewRepo.Setup(r => r.AddAsync(It.IsAny<Review>())).Returns(Task.CompletedTask);
        _reviewRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _reviewRepo.Setup(r => r.GetTotalCountByRestaurantAsync(restaurantId)).ReturnsAsync(1);
        _reviewRepo.Setup(r => r.GetRatingDistributionAsync(restaurantId))
                   .ReturnsAsync(new Dictionary<int, int> { { 5, 1 } });
        _restaurantRepo.Setup(r => r.GetByIdAsync(restaurantId)).ReturnsAsync(new Restaurant { Id = restaurantId });
        _restaurantRepo.Setup(r => r.UpdateAsync(It.IsAny<Restaurant>())).Returns(Task.CompletedTask);
        _restaurantRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var dto = new CreateReviewDto { Rating = 5, Comment = "Excellent!" };

        var result = await _sut.AddReviewAsync(restaurantId, userId, "John Doe", dto);

        Assert.That(result, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void AddReview_DuplicateReview_ThrowsInvalidOperation()
    {
        var restaurantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var existingReview = new Review { Id = Guid.NewGuid(), UserId = userId, RestaurantId = restaurantId };
        _reviewRepo.Setup(r => r.GetByUserAndRestaurantAsync(userId, restaurantId)).ReturnsAsync(existingReview);

        var dto = new CreateReviewDto { Rating = 4, Comment = "Good" };

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.AddReviewAsync(restaurantId, userId, "John", dto));
    }

    [Test]
    [TestCase(0)]
    [TestCase(6)]
    public void AddReview_InvalidRating_ThrowsArgumentException(int rating)
    {
        _reviewRepo.Setup(r => r.GetByUserAndRestaurantAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((Review?)null);

        var dto = new CreateReviewDto { Rating = rating, Comment = "Test" };

        Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.AddReviewAsync(Guid.NewGuid(), Guid.NewGuid(), "User", dto));
    }
}
