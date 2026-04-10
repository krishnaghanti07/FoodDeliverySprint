using System;
using System.Collections.Generic;
using System.Text;
namespace OrderService.Domain.Entities;

public class CartItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsVeg { get; set; }
    public Guid RestaurantId { get; set; }
}
