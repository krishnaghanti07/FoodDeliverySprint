using CatalogService.Application.DTOs;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Interfaces;

namespace CatalogService.Application.Services;

public class CatalogAppService
{
    private readonly IRestaurantRepository _restaurantRepo;
    private readonly IMenuItemRepository _menuItemRepo;
    private readonly ICategoryRepository _categoryRepo;
    private readonly IOperatingHourRepository _operatingHourRepo;
    private readonly IReviewRepository _reviewRepo;

    public CatalogAppService(
        IRestaurantRepository restaurantRepo,
        IMenuItemRepository menuItemRepo,
        ICategoryRepository categoryRepo,
        IOperatingHourRepository operatingHourRepo,
        IReviewRepository reviewRepo)
    {
        _restaurantRepo = restaurantRepo;
        _menuItemRepo = menuItemRepo;
        _categoryRepo = categoryRepo;
        _operatingHourRepo = operatingHourRepo;
        _reviewRepo = reviewRepo;
    }

    // ── Restaurants ────────────────────────────────────────────────────

    /// <summary>
    /// Get home page data with promoted restaurants and popular cuisines.
    /// PRD: GET /gateway/catalog/home
    /// </summary>
    public async Task<HomePageDto> GetHomePageDataAsync(string? city)
    {
        var allRestaurants = await _restaurantRepo.GetAllApprovedAsync(city, null, null);
        
        return new HomePageDto
        {
            PromotedRestaurants = allRestaurants
                .Where(r => r.IsOpen && r.Rating >= 4.0)
                .OrderByDescending(r => r.Rating)
                .Take(6)
                .Select(MapToListDto)
                .ToList(),
            PopularCuisines = allRestaurants
                .GroupBy(r => r.Cuisine)
                .OrderByDescending(g => g.Count())
                .Take(8)
                .Select(g => g.Key)
                .ToList(),
            TotalRestaurants = allRestaurants.Count
        };
    }

    /// <summary>
    /// Get nearby restaurants based on city (location-aware in production).
    /// PRD: GET /gateway/catalog/restaurants/nearby
    /// </summary>
    public async Task<List<RestaurantListDto>> GetNearbyRestaurantsAsync(string? city)
    {
        // In production, this would use geolocation
        // For now, filter by city and return open restaurants first
        var list = await _restaurantRepo.GetAllApprovedAsync(city, null, null);
        return list
            .OrderByDescending(r => r.IsOpen)
            .ThenByDescending(r => r.Rating)
            .Select(MapToListDto)
            .ToList();
    }

    public async Task<List<RestaurantListDto>> GetRestaurantsAsync(
        string? city, string? cuisine, string? search)
    {
        var list = await _restaurantRepo.GetAllApprovedAsync(city, cuisine, search);
        return list.Select(MapToListDto).ToList();
    }

    /// <summary>
    /// Enhanced search that searches both restaurants and menu items
    /// </summary>
    public async Task<SearchResultsDto> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new SearchResultsDto();

        // Search restaurants
        var restaurants = await _restaurantRepo.GetAllApprovedAsync(null, null, query);
        
        // Search menu items
        var menuItems = await _menuItemRepo.SearchByNameOrDescriptionAsync(query);

        var result = new SearchResultsDto
        {
            Restaurants = restaurants.Select(MapToListDto).ToList(),
            MenuItems = menuItems.Select(m => new MenuItemSearchDto
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                Price = m.Price,
                IsVeg = m.IsVeg,
                IsAvailable = m.IsAvailable,
                ImageUrl = m.ImageUrl,
                RestaurantId = m.RestaurantId,
                RestaurantName = m.Category.Restaurant.Name,
                RestaurantCuisine = m.Category.Restaurant.Cuisine,
                RestaurantCity = m.Category.Restaurant.City,
                RestaurantIsOpen = m.Category.Restaurant.IsOpen
            }).ToList(),
            TotalResults = restaurants.Count + menuItems.Count
        };

        return result;
    }

    public async Task<List<RestaurantListDto>> GetAllIncludingUnapprovedAsync()
    {
        var list = await _restaurantRepo.GetAllAsync();
        return list.Select(MapToListDto).ToList();
    }

    public async Task<List<RestaurantListDto>> GetRestaurantsByPartnerIdAsync(Guid partnerId)
    {
        var restaurant = await _restaurantRepo.GetByPartnerIdAsync(partnerId);
        if (restaurant == null)
            return new List<RestaurantListDto>();
        
        return new List<RestaurantListDto> { MapToListDto(restaurant) };
    }

    public async Task<RestaurantDetailDto?> GetRestaurantWithMenuAsync(Guid id)
    {
        var r = await _restaurantRepo.GetByIdWithMenuAsync(id);
        return r is null ? null : MapToDetailDto(r);
    }

    public async Task<RestaurantDetailDto?> GetRestaurantWithMenuAdminAsync(Guid id)
    {
        var r = await _restaurantRepo.GetByIdWithMenuAdminAsync(id);
        return r is null ? null : MapToDetailDto(r);
    }

    public async Task<Guid> CreateRestaurantAsync(CreateRestaurantDto dto, Guid partnerId)
    {
        // Validation: Check if partner already has an active restaurant
        var existingActive = await _restaurantRepo.GetByPartnerIdAsync(partnerId);
        if (existingActive != null)
        {
            throw new InvalidOperationException(
                "You already have an active restaurant. Each partner can only manage one restaurant at a time. " +
                "Please contact support if you need assistance.");
        }

        // Validation: Check if partner has a deleted restaurant
        var existingDeleted = await _restaurantRepo.GetDeletedByPartnerIdAsync(partnerId);
        if (existingDeleted != null)
        {
            throw new InvalidOperationException(
                $"You have a deleted restaurant '{existingDeleted.Name}'. " +
                "Please contact admin to restore it or permanently remove it before creating a new one.");
        }

        var restaurant = new Restaurant
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            Cuisine = dto.Cuisine,
            Address = dto.Address,
            City = dto.City,
            Phone = dto.Phone,
            LogoUrl = dto.LogoUrl,
            PrepTimeMinutes = dto.PrepTimeMinutes,
            MinOrderAmount = dto.MinOrderAmount,
            DeliveryFee = dto.DeliveryFee,
            PartnerUserId = partnerId,
            IsApproved = false
        };
        await _restaurantRepo.AddAsync(restaurant);
        await _restaurantRepo.SaveChangesAsync();
        return restaurant.Id;
    }

    public async Task UpdateRestaurantAsync(
        Guid id, CreateRestaurantDto dto, Guid requesterId, string role)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Restaurant not found.");

        if (role == "Partner" && restaurant.PartnerUserId != requesterId)
            throw new UnauthorizedAccessException(
                "You can only update your own restaurant.");

        restaurant.Name = dto.Name.Trim();
        restaurant.Description = dto.Description;
        restaurant.Cuisine = dto.Cuisine;
        restaurant.Address = dto.Address;
        restaurant.City = dto.City;
        restaurant.Phone = dto.Phone;
        restaurant.LogoUrl = dto.LogoUrl;
        restaurant.PrepTimeMinutes = dto.PrepTimeMinutes;
        restaurant.MinOrderAmount = dto.MinOrderAmount;
        restaurant.DeliveryFee = dto.DeliveryFee;

        await _restaurantRepo.UpdateAsync(restaurant);
        await _restaurantRepo.SaveChangesAsync();
    }

    public async Task<bool> ToggleOpenStatusAsync(Guid id, Guid partnerId)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Restaurant not found.");

        if (restaurant.PartnerUserId != partnerId)
            throw new UnauthorizedAccessException("Access denied.");

        restaurant.IsOpen = !restaurant.IsOpen;
        await _restaurantRepo.UpdateAsync(restaurant);
        await _restaurantRepo.SaveChangesAsync();
        return restaurant.IsOpen;
    }

    public async Task ApproveRestaurantAsync(Guid id)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Restaurant not found.");
        restaurant.IsApproved = true;
        await _restaurantRepo.UpdateAsync(restaurant);
        await _restaurantRepo.SaveChangesAsync();
    }

    public async Task UpdateRestaurantStatusAsync(Guid id, string status)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Restaurant not found.");
        
        // Update IsApproved based on status
        restaurant.IsApproved = status == "Approved";
        
        // You can add a Status field to Restaurant entity if needed
        // For now, we'll just update IsApproved
        await _restaurantRepo.UpdateAsync(restaurant);
        await _restaurantRepo.SaveChangesAsync();
    }

    public async Task DeleteRestaurantAsync(Guid id, Guid requesterId, string role)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Restaurant not found.");

        if (role == "Partner" && restaurant.PartnerUserId != requesterId)
            throw new UnauthorizedAccessException(
                "You can only delete your own restaurant.");

        // Set soft delete fields
        restaurant.IsDeleted = true;
        restaurant.DeletedAt = DateTime.UtcNow;
        restaurant.DeletedBy = requesterId;
        restaurant.DeletionReason = "Deleted by " + role;

        await _restaurantRepo.UpdateAsync(restaurant);
        await _restaurantRepo.SaveChangesAsync();
    }

    public async Task RestoreRestaurantAsync(Guid id, Guid restoredBy, string reason)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Restaurant not found.");

        if (!restaurant.IsDeleted)
            throw new InvalidOperationException("Restaurant is not deleted.");

        // Validation: Check if partner already has an active restaurant
        var existingActive = await _restaurantRepo.GetByPartnerIdAsync(restaurant.PartnerUserId);
        if (existingActive != null)
        {
            throw new InvalidOperationException(
                $"Cannot restore restaurant: The partner already has an active restaurant '{existingActive.Name}' (ID: {existingActive.Id}). " +
                "Please delete or archive the current restaurant before restoring this one. " +
                "Each partner can only have one active restaurant at a time.");
        }

        // Restore the restaurant
        restaurant.IsDeleted = false;
        restaurant.DeletedAt = null;
        restaurant.DeletedBy = null;
        restaurant.DeletionReason = null;

        await _restaurantRepo.UpdateAsync(restaurant);
        await _restaurantRepo.SaveChangesAsync();
    }

    public async Task PermanentlyDeleteRestaurantAsync(Guid id, Guid deletedBy, string role)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Restaurant not found.");

        if (!restaurant.IsDeleted)
            throw new InvalidOperationException("Restaurant must be soft-deleted before permanent deletion. Please soft-delete it first.");

        // Only Admin can permanently delete
        if (role != "Admin")
            throw new UnauthorizedAccessException("Only administrators can permanently delete restaurants.");

        // Permanently delete the restaurant (hard delete)
        await _restaurantRepo.PermanentlyDeleteAsync(id);
        await _restaurantRepo.SaveChangesAsync();
    }

    // ── Menu Items ─────────────────────────────────────────────────────

    public async Task<List<MenuItemDto>> GetMenuItemsByRestaurantAsync(Guid restaurantId)
    {
        var items = await _menuItemRepo.GetByRestaurantIdAsync(restaurantId);
        return items.Select(m => new MenuItemDto
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            Price = m.Price,
            IsVeg = m.IsVeg,
            IsAvailable = m.IsAvailable,
            ImageUrl = m.ImageUrl,
            CategoryId = m.CategoryId
        }).ToList();
    }

    public async Task<MenuItemDto?> GetMenuItemByIdAsync(Guid id)
    {
        var item = await _menuItemRepo.GetByIdAsync(id);
        if (item is null) return null;
        
        return new MenuItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price,
            IsVeg = item.IsVeg,
            IsAvailable = item.IsAvailable,
            ImageUrl = item.ImageUrl,
            CategoryId = item.CategoryId
        };
    }

    public async Task<Guid> AddMenuItemAsync(CreateMenuItemDto dto)
    {
        var item = new MenuItem
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            Price = dto.Price,
            IsVeg = dto.IsVeg,
            ImageUrl = dto.ImageUrl,
            CategoryId = dto.CategoryId,
            RestaurantId = dto.RestaurantId
        };
        await _menuItemRepo.AddAsync(item);
        await _menuItemRepo.SaveChangesAsync();
        return item.Id;
    }

    public async Task UpdateMenuItemAsync(Guid id, CreateMenuItemDto dto)
    {
        var item = await _menuItemRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Menu item not found.");

        item.Name = dto.Name.Trim();
        item.Description = dto.Description;
        item.Price = dto.Price;
        item.IsVeg = dto.IsVeg;
        item.ImageUrl = dto.ImageUrl;

        await _menuItemRepo.UpdateAsync(item);
        await _menuItemRepo.SaveChangesAsync();
    }

    public async Task<bool> ToggleMenuItemAvailabilityAsync(Guid id)
    {
        var item = await _menuItemRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Menu item not found.");
        item.IsAvailable = !item.IsAvailable;
        await _menuItemRepo.UpdateAsync(item);
        await _menuItemRepo.SaveChangesAsync();
        return item.IsAvailable;
    }

    public async Task DeleteMenuItemAsync(Guid id)
    {
        var item = await _menuItemRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Menu item not found.");
        await _menuItemRepo.DeleteAsync(id);
        await _menuItemRepo.SaveChangesAsync();
    }

    // ── Mappings ───────────────────────────────────────────────────────

    private static RestaurantListDto MapToListDto(Restaurant r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Cuisine = r.Cuisine,
        City = r.City,
        LogoUrl = r.LogoUrl,
        Rating = r.Rating,
        PrepTimeMinutes = r.PrepTimeMinutes,
        DeliveryFee = r.DeliveryFee,
        IsOpen = r.IsOpen,
        IsApproved = r.IsApproved,
        IsDeleted = r.IsDeleted,
        DeletionReason = r.DeletionReason
    };

    private static RestaurantDetailDto MapToDetailDto(Restaurant r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        Cuisine = r.Cuisine,
        City = r.City,
        Address = r.Address,
        Phone = r.Phone,
        LogoUrl = r.LogoUrl,
        Rating = r.Rating,
        PrepTimeMinutes = r.PrepTimeMinutes,
        DeliveryFee = r.DeliveryFee,
        MinOrderAmount = r.MinOrderAmount,
        IsOpen = r.IsOpen,
        IsApproved = r.IsApproved,
        Categories = r.Categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                DisplayOrder = c.DisplayOrder,
                MenuItems = c.MenuItems
                    .Where(m => m.IsAvailable)
                    .Select(m => new MenuItemDto
                    {
                        Id = m.Id,
                        Name = m.Name,
                        Description = m.Description,
                        Price = m.Price,
                        IsVeg = m.IsVeg,
                        IsAvailable = m.IsAvailable,
                        ImageUrl = m.ImageUrl
                    }).ToList()
            }).ToList()
    };

    // ── Categories ─────────────────────────────────────────────────────

    public async Task<List<CategoryDto>> GetCategoriesByRestaurantAsync(Guid restaurantId)
    {
        var categories = await _categoryRepo.GetByRestaurantIdAsync(restaurantId);
        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            DisplayOrder = c.DisplayOrder,
            MenuItems = new List<MenuItemDto>()  // Empty for list view
        }).ToList();
    }

    public async Task<Guid> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var category = new Category
        {
            Name = dto.Name.Trim(),
            DisplayOrder = dto.DisplayOrder,
            RestaurantId = dto.RestaurantId
        };
        await _categoryRepo.AddAsync(category);
        await _categoryRepo.SaveChangesAsync();
        return category.Id;
    }

    public async Task UpdateCategoryAsync(Guid id, UpdateCategoryDto dto)
    {
        var category = await _categoryRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Category not found.");
        
        category.Name = dto.Name.Trim();
        category.DisplayOrder = dto.DisplayOrder;
        
        await _categoryRepo.UpdateAsync(category);
        await _categoryRepo.SaveChangesAsync();
    }

    public async Task DeleteCategoryAsync(Guid id)
    {
        await _categoryRepo.DeleteAsync(id);
        await _categoryRepo.SaveChangesAsync();
    }

    public async Task ReorderCategoriesAsync(Guid restaurantId, ReorderCategoriesDto dto)
    {
        var categories = await _categoryRepo.GetByRestaurantIdAsync(restaurantId);
        
        foreach (var orderDto in dto.Categories)
        {
            var category = categories.FirstOrDefault(c => c.Id == orderDto.CategoryId);
            if (category != null)
            {
                category.DisplayOrder = orderDto.DisplayOrder;
                await _categoryRepo.UpdateAsync(category);
            }
        }
        
        await _categoryRepo.SaveChangesAsync();
    }

    // ── Operating Hours ────────────────────────────────────────────────

    public async Task<List<OperatingHourDto>> GetOperatingHoursAsync(Guid restaurantId)
    {
        var hours = await _operatingHourRepo.GetByRestaurantIdAsync(restaurantId);
        return hours.Select(h => new OperatingHourDto
        {
            Id = h.Id,
            DayOfWeek = h.DayOfWeek,
            DayName = ((DayOfWeek)h.DayOfWeek).ToString(),
            OpenTime = h.OpenTime,
            CloseTime = h.CloseTime,
            IsClosed = h.IsClosed
        }).ToList();
    }

    public async Task SetOperatingHoursAsync(Guid restaurantId, List<CreateOperatingHourDto> dtos)
    {
        // Delete existing hours
        await _operatingHourRepo.DeleteByRestaurantIdAsync(restaurantId);
        
        // Add new hours
        var hours = dtos.Select(dto => new OperatingHour
        {
            RestaurantId = restaurantId,
            DayOfWeek = dto.DayOfWeek,
            OpenTime = dto.OpenTime,
            CloseTime = dto.CloseTime,
            IsClosed = dto.IsClosed
        }).ToList();
        
        await _operatingHourRepo.AddRangeAsync(hours);
        await _operatingHourRepo.SaveChangesAsync();
    }

    // ── Reviews & Ratings ──────────────────────────────────────────────

    public async Task<List<ReviewDto>> GetReviewsAsync(Guid restaurantId, int page = 1, int pageSize = 10)
    {
        var reviews = await _reviewRepo.GetByRestaurantIdAsync(restaurantId, page, pageSize);
        return reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            RestaurantId = r.RestaurantId,
            UserId = r.UserId,
            UserName = r.UserName,
            Rating = r.Rating,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            HelpfulCount = r.HelpfulCount
        }).ToList();
    }

    public async Task<RestaurantRatingsSummaryDto> GetRatingsSummaryAsync(Guid restaurantId)
    {
        var totalReviews = await _reviewRepo.GetTotalCountByRestaurantAsync(restaurantId);
        var distribution = await _reviewRepo.GetRatingDistributionAsync(restaurantId);
        
        double averageRating = 0;
        if (totalReviews > 0)
        {
            int totalStars = distribution.Sum(kvp => kvp.Key * kvp.Value);
            averageRating = Math.Round((double)totalStars / totalReviews, 1);
        }
        
        return new RestaurantRatingsSummaryDto
        {
            AverageRating = averageRating,
            TotalReviews = totalReviews,
            RatingDistribution = distribution
        };
    }

    public async Task<Guid> AddReviewAsync(Guid restaurantId, Guid userId, string userName, CreateReviewDto dto)
    {
        // Check if user already reviewed this restaurant
        var existing = await _reviewRepo.GetByUserAndRestaurantAsync(userId, restaurantId);
        if (existing != null)
            throw new InvalidOperationException("You have already reviewed this restaurant. Use update instead.");
        
        if (dto.Rating < 1 || dto.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");
        
        var review = new Review
        {
            RestaurantId = restaurantId,
            UserId = userId,
            UserName = userName,
            Rating = dto.Rating,
            Comment = dto.Comment.Trim()
        };
        
        await _reviewRepo.AddAsync(review);
        await _reviewRepo.SaveChangesAsync();
        
        // Update restaurant rating
        await UpdateRestaurantRatingAsync(restaurantId);
        
        return review.Id;
    }

    public async Task UpdateReviewAsync(Guid reviewId, Guid userId, UpdateReviewDto dto)
    {
        var review = await _reviewRepo.GetByIdAsync(reviewId)
            ?? throw new KeyNotFoundException("Review not found.");
        
        if (review.UserId != userId)
            throw new UnauthorizedAccessException("You can only update your own reviews.");
        
        if (dto.Rating < 1 || dto.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");
        
        review.Rating = dto.Rating;
        review.Comment = dto.Comment.Trim();
        review.UpdatedAt = DateTime.UtcNow;
        
        await _reviewRepo.UpdateAsync(review);
        await _reviewRepo.SaveChangesAsync();
        
        // Update restaurant rating
        await UpdateRestaurantRatingAsync(review.RestaurantId);
    }

    public async Task DeleteReviewAsync(Guid reviewId, Guid userId, string userRole)
    {
        var review = await _reviewRepo.GetByIdAsync(reviewId)
            ?? throw new KeyNotFoundException("Review not found.");
        
        if (userRole != "Admin" && review.UserId != userId)
            throw new UnauthorizedAccessException("You can only delete your own reviews.");
        
        var restaurantId = review.RestaurantId;
        
        await _reviewRepo.DeleteAsync(reviewId);
        await _reviewRepo.SaveChangesAsync();
        
        // Update restaurant rating
        await UpdateRestaurantRatingAsync(restaurantId);
    }

    public async Task MarkReviewHelpfulAsync(Guid reviewId)
    {
        var review = await _reviewRepo.GetByIdAsync(reviewId)
            ?? throw new KeyNotFoundException("Review not found.");
        
        review.HelpfulCount++;
        await _reviewRepo.UpdateAsync(review);
        await _reviewRepo.SaveChangesAsync();
    }

    private async Task UpdateRestaurantRatingAsync(Guid restaurantId)
    {
        var summary = await GetRatingsSummaryAsync(restaurantId);
        var restaurant = await _restaurantRepo.GetByIdAsync(restaurantId);
        
        if (restaurant != null)
        {
            restaurant.Rating = summary.AverageRating;
            await _restaurantRepo.UpdateAsync(restaurant);
            await _restaurantRepo.SaveChangesAsync();
        }
    }

    // ── Rating Sync ────────────────────────────────────────────────────

    public async Task UpdateRestaurantRatingAsync(Guid restaurantId, double rating)
    {
        var restaurant = await _restaurantRepo.GetByIdAsync(restaurantId)
            ?? throw new KeyNotFoundException("Restaurant not found.");
        
        restaurant.Rating = rating;
        await _restaurantRepo.UpdateAsync(restaurant);
        await _restaurantRepo.SaveChangesAsync();
    }

}
