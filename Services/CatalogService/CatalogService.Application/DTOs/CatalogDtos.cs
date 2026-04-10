namespace CatalogService.Application.DTOs;

public class RestaurantListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Cuisine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public double Rating { get; set; }
    public int PrepTimeMinutes { get; set; }
    public decimal DeliveryFee { get; set; }
    public bool IsOpen { get; set; }
    public bool IsApproved { get; set; }
}

public class RestaurantDetailDto : RestaurantListDto
{
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal MinOrderAmount { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public List<MenuItemDto> MenuItems { get; set; } = new();
}

public class MenuItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsVeg { get; set; }
    public bool IsAvailable { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public class CreateRestaurantDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Cuisine { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; } = 30;
    public decimal MinOrderAmount { get; set; }
    public decimal DeliveryFee { get; set; }
}

public class CreateMenuItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsVeg { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid RestaurantId { get; set; }
}