using System;
using System.Collections.Generic;
using System.Text;
namespace OrderService.Domain.Entities;

public class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Guid? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public string? CouponCode { get; set; }
    public decimal Discount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
