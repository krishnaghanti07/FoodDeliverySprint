using System;
using System.Collections.Generic;
using System.Text;
namespace CatalogService.Domain.Entities;

public class Restaurant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Cuisine { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public double Rating { get; set; } = 0.0;
    public int PrepTimeMinutes { get; set; } = 30;
    public decimal MinOrderAmount { get; set; } = 0;
    public decimal DeliveryFee { get; set; } = 0;
    public bool IsOpen { get; set; } = true;
    public bool IsApproved { get; set; } = false;
    public Guid PartnerUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ── Soft Delete fields ────────────────────────────
    public bool IsDeleted { get; set; } = false;
    public Guid? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletionReason { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<OperatingHour> OperatingHours { get; set; } = new List<OperatingHour>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}

public class MenuItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsVeg { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string ImageUrl { get; set; } = string.Empty;
    public List<string> DietaryTags { get; set; } = new();  // Vegan, GlutenFree, Halal, etc.
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid RestaurantId { get; set; }
}

public class OperatingHour
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public int DayOfWeek { get; set; }  // 0=Sunday, 6=Saturday
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsClosed { get; set; } = false;
}

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;  // Denormalized for performance
    public int Rating { get; set; }  // 1-5
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public int HelpfulCount { get; set; } = 0;
}