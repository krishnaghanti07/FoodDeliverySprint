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
    public Guid PartnerUserId { get; set; }
    public bool IsDeleted { get; set; }
    public string? DeletionReason { get; set; }
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
    public Guid CategoryId { get; set; }
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

/// <summary>
/// Category management DTOs
/// </summary>
public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public Guid RestaurantId { get; set; }
}

public class UpdateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

public class ReorderCategoriesDto
{
    public List<CategoryOrderDto> Categories { get; set; } = new();
}

public class CategoryOrderDto
{
    public Guid CategoryId { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Home page data with promoted restaurants and popular cuisines.
/// PRD: GET /gateway/catalog/home
/// </summary>
public class HomePageDto
{
    public List<RestaurantListDto> PromotedRestaurants { get; set; } = new();
    public List<string> PopularCuisines { get; set; } = new();
    public int TotalRestaurants { get; set; }
}

/// <summary>
/// Enhanced search results including both restaurants and menu items
/// </summary>
public class SearchResultsDto
{
    public List<RestaurantListDto> Restaurants { get; set; } = new();
    public List<MenuItemSearchDto> MenuItems { get; set; } = new();
    public int TotalResults { get; set; }
}

/// <summary>
/// Menu item with restaurant information for search results
/// </summary>
public class MenuItemSearchDto : MenuItemDto
{
    public Guid RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string RestaurantCuisine { get; set; } = string.Empty;
    public string RestaurantCity { get; set; } = string.Empty;
    public bool RestaurantIsOpen { get; set; }
}

/// <summary>
/// Operating Hours DTOs
/// </summary>
public class OperatingHourDto
{
    public Guid Id { get; set; }
    public int DayOfWeek { get; set; }  // 0=Sunday, 6=Saturday
    public string DayName { get; set; } = string.Empty;
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsClosed { get; set; }
}

public class CreateOperatingHourDto
{
    public int DayOfWeek { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsClosed { get; set; }
}

/// <summary>
/// Review and Rating DTOs
/// </summary>
public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid RestaurantId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int HelpfulCount { get; set; }
}

public class CreateReviewDto
{
    public int Rating { get; set; }  // 1-5
    public string Comment { get; set; } = string.Empty;
}

public class UpdateReviewDto
{
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class RestaurantRatingsSummaryDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new();  // Star -> Count
}