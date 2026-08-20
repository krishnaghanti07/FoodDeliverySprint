using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Application.DTOs;
using OrderService.Application.Services;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Tests;

// ══════════════════════════════════════════════════════════════════════
// CART SERVICE — UNIT TESTS
// Covers: Add/Update/Remove items, Apply/Remove coupon, Checkout context
// ══════════════════════════════════════════════════════════════════════
[TestFixture]
public class CartServiceTests
{
    private Mock<ICartRepository> _cartRepo = null!;
    private Mock<ILogger<CartAppService>> _logger = null!;
    private CartAppService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _cartRepo = new Mock<ICartRepository>();
        _logger = new Mock<ILogger<CartAppService>>();
        _sut = new CartAppService(_cartRepo.Object, _logger.Object);
    }

    // ── Get Cart ──────────────────────────────────────────────────────

    [Test]
    public async Task GetCart_NoExistingCart_ReturnsEmptyCart()
    {
        var customerId = Guid.NewGuid();
        _cartRepo.Setup(r => r.GetByCustomerIdAsync(customerId)).ReturnsAsync((Cart?)null);

        var result = await _sut.GetCartAsync(customerId);

        Assert.That(result.CustomerId, Is.EqualTo(customerId));
        Assert.That(result.Items, Is.Empty);
        Assert.That(result.Total, Is.EqualTo(0));
    }

    [Test]
    public async Task GetCart_ExistingCart_ReturnsCartWithItems()
    {
        var customerId = Guid.NewGuid();
        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            RestaurantId = Guid.NewGuid(),
            Items = new List<CartItem>
            {
                new() { Id = Guid.NewGuid(), MenuItemId = Guid.NewGuid(), Name = "Pizza", Quantity = 2, UnitPrice = 250m, IsVeg = false },
                new() { Id = Guid.NewGuid(), MenuItemId = Guid.NewGuid(), Name = "Coke", Quantity = 1, UnitPrice = 50m, IsVeg = true }
            }
        };
        _cartRepo.Setup(r => r.GetByCustomerIdAsync(customerId)).ReturnsAsync(cart);

        var result = await _sut.GetCartAsync(customerId);

        Assert.That(result.Items.Count, Is.EqualTo(2));
        Assert.That(result.Subtotal, Is.EqualTo(550m)); // 250*2 + 50*1
        Assert.That(result.ItemCount, Is.EqualTo(3)); // 2 + 1
    }

    // ── Add Item ──────────────────────────────────────────────────────

    [Test]
    public async Task AddItem_NewCart_CreatesCartWithItem()
    {
        var customerId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();
        _cartRepo.Setup(r => r.GetByCustomerIdNoTrackingAsync(customerId)).ReturnsAsync((Cart?)null);
        _cartRepo.Setup(r => r.AddAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
        _cartRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        Cart? capturedCart = null;
        _cartRepo.Setup(r => r.AddAsync(It.IsAny<Cart>()))
                 .Callback<Cart>(c => capturedCart = c)
                 .Returns(Task.CompletedTask);

        var dto = new AddCartItemDto
        {
            RestaurantId = restaurantId,
            MenuItemId = Guid.NewGuid(),
            ItemName = "Burger",
            Quantity = 1,
            UnitPrice = 150m,
            IsVeg = false
        };

        var result = await _sut.AddItemAsync(customerId, dto);

        Assert.That(capturedCart, Is.Not.Null);
        Assert.That(capturedCart!.Items.Count, Is.EqualTo(1));
        Assert.That(capturedCart.Items.First().Name, Is.EqualTo("Burger"));
        Assert.That(result.Subtotal, Is.EqualTo(150m));
    }

    [Test]
    public async Task AddItem_ExistingItem_IncreasesQuantity()
    {
        var customerId = Guid.NewGuid();
        var menuItemId = Guid.NewGuid();
        var existingCart = new Cart
        {
            CustomerId = customerId,
            RestaurantId = Guid.NewGuid(),
            Items = new List<CartItem>
            {
                new() { MenuItemId = menuItemId, Name = "Pizza", Quantity = 1, UnitPrice = 200m, IsVeg = false }
            }
        };
        _cartRepo.Setup(r => r.GetByCustomerIdNoTrackingAsync(customerId)).ReturnsAsync(existingCart);
        _cartRepo.Setup(r => r.DeleteAsync(customerId)).Returns(Task.CompletedTask);
        _cartRepo.Setup(r => r.AddAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
        _cartRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        Cart? capturedCart = null;
        _cartRepo.Setup(r => r.AddAsync(It.IsAny<Cart>()))
                 .Callback<Cart>(c => capturedCart = c)
                 .Returns(Task.CompletedTask);

        var dto = new AddCartItemDto
        {
            RestaurantId = existingCart.RestaurantId!.Value,
            MenuItemId = menuItemId,
            ItemName = "Pizza",
            Quantity = 2,
            UnitPrice = 200m,
            IsVeg = false
        };

        await _sut.AddItemAsync(customerId, dto);

        Assert.That(capturedCart, Is.Not.Null);
        Assert.That(capturedCart!.Items.Count, Is.EqualTo(1));
        Assert.That(capturedCart.Items.First().Quantity, Is.EqualTo(3)); // 1 + 2
    }

    // ── Update Item ───────────────────────────────────────────────────

    [Test]
    public async Task UpdateItem_ValidItem_UpdatesQuantity()
    {
        var customerId = Guid.NewGuid();
        var cartItemId = Guid.NewGuid();
        var cart = new Cart
        {
            CustomerId = customerId,
            Items = new List<CartItem>
            {
                new() { Id = cartItemId, MenuItemId = Guid.NewGuid(), Name = "Pasta", Quantity = 2, UnitPrice = 180m }
            }
        };
        _cartRepo.Setup(r => r.GetByCustomerIdAsync(customerId)).ReturnsAsync(cart);
        _cartRepo.Setup(r => r.UpdateAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
        _cartRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.UpdateItemAsync(customerId, cartItemId, new UpdateCartItemDto { Quantity = 5 });

        Assert.That(cart.Items.First().Quantity, Is.EqualTo(5));
        Assert.That(result.Subtotal, Is.EqualTo(900m)); // 180 * 5
    }

    [Test]
    public async Task UpdateItem_QuantityZero_RemovesItem()
    {
        var customerId = Guid.NewGuid();
        var cartItemId = Guid.NewGuid();
        var cart = new Cart
        {
            CustomerId = customerId,
            RestaurantId = Guid.NewGuid(),
            Items = new List<CartItem>
            {
                new() { Id = cartItemId, MenuItemId = Guid.NewGuid(), Name = "Fries", Quantity = 1, UnitPrice = 80m }
            }
        };
        _cartRepo.Setup(r => r.GetByCustomerIdAsync(customerId)).ReturnsAsync(cart);
        _cartRepo.Setup(r => r.UpdateAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
        _cartRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.UpdateItemAsync(customerId, cartItemId, new UpdateCartItemDto { Quantity = 0 });

        Assert.That(cart.Items, Is.Empty);
        Assert.That(cart.RestaurantId, Is.Null, "RestaurantId should be cleared when cart is empty");
    }

    [Test]
    public void UpdateItem_NonExistentCart_ThrowsKeyNotFoundException()
    {
        _cartRepo.Setup(r => r.GetByCustomerIdAsync(It.IsAny<Guid>())).ReturnsAsync((Cart?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.UpdateItemAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateCartItemDto { Quantity = 1 }));
    }

    [Test]
    public void UpdateItem_NonExistentItem_ThrowsKeyNotFoundException()
    {
        var cart = new Cart { CustomerId = Guid.NewGuid(), Items = new List<CartItem>() };
        _cartRepo.Setup(r => r.GetByCustomerIdAsync(It.IsAny<Guid>())).ReturnsAsync(cart);

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.UpdateItemAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateCartItemDto { Quantity = 1 }));
    }

    // ── Remove Item ───────────────────────────────────────────────────

    [Test]
    public async Task RemoveItem_ValidItem_RemovesFromCart()
    {
        var customerId = Guid.NewGuid();
        var cartItemId = Guid.NewGuid();
        var cart = new Cart
        {
            CustomerId = customerId,
            RestaurantId = Guid.NewGuid(),
            Items = new List<CartItem>
            {
                new() { Id = cartItemId, MenuItemId = Guid.NewGuid(), Name = "Salad", Quantity = 1, UnitPrice = 120m },
                new() { Id = Guid.NewGuid(), MenuItemId = Guid.NewGuid(), Name = "Juice", Quantity = 1, UnitPrice = 60m }
            }
        };
        _cartRepo.Setup(r => r.GetByCustomerIdAsync(customerId)).ReturnsAsync(cart);
        _cartRepo.Setup(r => r.UpdateAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
        _cartRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.RemoveItemAsync(customerId, cartItemId);

        Assert.That(cart.Items.Count, Is.EqualTo(1));
        Assert.That(cart.Items.First().Name, Is.EqualTo("Juice"));
    }

    // ── Apply Coupon ──────────────────────────────────────────────────

    [Test]
    public async Task ApplyCoupon_ValidCode_AppliesDiscount()
    {
        var customerId = Guid.NewGuid();
        var cart = new Cart
        {
            CustomerId = customerId,
            Items = new List<CartItem>
            {
                new() { MenuItemId = Guid.NewGuid(), Name = "Biryani", Quantity = 1, UnitPrice = 250m }
            }
        };
        _cartRepo.Setup(r => r.GetByCustomerIdAsync(customerId)).ReturnsAsync(cart);
        _cartRepo.Setup(r => r.UpdateAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
        _cartRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.ApplyCouponAsync(customerId, new ApplyCouponDto { CouponCode = "FLAT50" });

        Assert.That(cart.CouponCode, Is.EqualTo("FLAT50"));
        Assert.That(cart.Discount, Is.EqualTo(50m));
        Assert.That(result.Total, Is.EqualTo(200m)); // 250 - 50
    }

    [Test]
    public void ApplyCoupon_EmptyCart_ThrowsInvalidOperation()
    {
        var cart = new Cart { CustomerId = Guid.NewGuid(), Items = new List<CartItem>() };
        _cartRepo.Setup(r => r.GetByCustomerIdAsync(It.IsAny<Guid>())).ReturnsAsync(cart);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ApplyCouponAsync(Guid.NewGuid(), new ApplyCouponDto { CouponCode = "FLAT50" }));
    }

    [Test]
    public void ApplyCoupon_InvalidCode_ThrowsInvalidOperation()
    {
        var cart = new Cart
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<CartItem>
            {
                new() { MenuItemId = Guid.NewGuid(), Name = "Item", Quantity = 1, UnitPrice = 50m }
            }
        };
        _cartRepo.Setup(r => r.GetByCustomerIdAsync(It.IsAny<Guid>())).ReturnsAsync(cart);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ApplyCouponAsync(Guid.NewGuid(), new ApplyCouponDto { CouponCode = "INVALID" }));
    }

    // ── Remove Coupon ─────────────────────────────────────────────────

    [Test]
    public async Task RemoveCoupon_AppliedCoupon_ClearsDiscount()
    {
        var customerId = Guid.NewGuid();
        var cart = new Cart
        {
            CustomerId = customerId,
            CouponCode = "SAVE20",
            Discount = 40m,
            Items = new List<CartItem>
            {
                new() { MenuItemId = Guid.NewGuid(), Name = "Item", Quantity = 1, UnitPrice = 200m }
            }
        };
        _cartRepo.Setup(r => r.GetByCustomerIdAsync(customerId)).ReturnsAsync(cart);
        _cartRepo.Setup(r => r.UpdateAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);
        _cartRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.RemoveCouponAsync(customerId);

        Assert.That(cart.CouponCode, Is.Null);
        Assert.That(cart.Discount, Is.EqualTo(0));
    }

    // ── Clear Cart ────────────────────────────────────────────────────

    [Test]
    public async Task ClearCart_ExistingCart_DeletesCart()
    {
        var customerId = Guid.NewGuid();
        _cartRepo.Setup(r => r.DeleteAsync(customerId)).Returns(Task.CompletedTask);
        _cartRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.ClearCartAsync(customerId);

        _cartRepo.Verify(r => r.DeleteAsync(customerId), Times.Once);
        _cartRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    // ── Checkout Context ──────────────────────────────────────────────

    [Test]
    public async Task GetCheckoutContext_ValidCart_ReturnsContextWithFees()
    {
        var customerId = Guid.NewGuid();
        var cart = new Cart
        {
            CustomerId = customerId,
            Items = new List<CartItem>
            {
                new() { MenuItemId = Guid.NewGuid(), Name = "Thali", Quantity = 2, UnitPrice = 180m }
            },
            Discount = 0
        };
        _cartRepo.Setup(r => r.GetByCustomerIdAsync(customerId)).ReturnsAsync(cart);

        var result = await _sut.GetCheckoutContextAsync(customerId);

        Assert.That(result.Cart.Subtotal, Is.EqualTo(360m)); // 180 * 2
        Assert.That(result.DeliveryFee, Is.EqualTo(30m));
        Assert.That(result.GstAmount, Is.EqualTo(18m)); // 5% of 360
        Assert.That(result.PlatformFee, Is.EqualTo(15m));
        Assert.That(result.TotalAmount, Is.EqualTo(423m)); // 360 + 30 + 18 + 15
    }

    [Test]
    public void GetCheckoutContext_EmptyCart_ThrowsInvalidOperation()
    {
        var cart = new Cart { CustomerId = Guid.NewGuid(), Items = new List<CartItem>() };
        _cartRepo.Setup(r => r.GetByCustomerIdAsync(It.IsAny<Guid>())).ReturnsAsync(cart);

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.GetCheckoutContextAsync(Guid.NewGuid()));
    }

    [Test]
    public void GetCheckoutContext_NoCart_ThrowsKeyNotFoundException()
    {
        _cartRepo.Setup(r => r.GetByCustomerIdAsync(It.IsAny<Guid>())).ReturnsAsync((Cart?)null);

        Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _sut.GetCheckoutContextAsync(Guid.NewGuid()));
    }
}
