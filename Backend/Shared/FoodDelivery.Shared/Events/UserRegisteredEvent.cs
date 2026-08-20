using System;
using System.Collections.Generic;
using System.Text;
namespace FoodDelivery.Shared.Events;

public class UserRegisteredEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
