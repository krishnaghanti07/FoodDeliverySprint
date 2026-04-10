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

    public ICollection<Category> Categories { get; set; } = new List<Category>();
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
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public Guid RestaurantId { get; set; }
}