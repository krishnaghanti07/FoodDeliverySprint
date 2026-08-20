using System;

namespace OrderService.Domain.Entities;

public class OrderRating
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public int FoodRating { get; set; }  // 1-5
    public int DeliveryRating { get; set; }  // 1-5
    public string? Comment { get; set; }
    public string? Tags { get; set; }  // JSON array: ["Cold food", "Late delivery"]
    public string? Photos { get; set; }  // JSON array of photo URLs
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
