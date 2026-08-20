using CatalogService.Domain.Entities;
using CatalogService.Infrastructure.Persistence;

namespace CatalogService.Infrastructure.Data;

public static class DataSeeder
{
    public static void SeedData(CatalogDbContext context)
    {
        // Check if data already exists
        if (context.Restaurants.Any())
        {
            Console.WriteLine("✅ Catalog data already seeded");
            return;
        }

        var restaurants = new List<Restaurant>
        {
            new Restaurant
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Name = "Mario's Italian Kitchen",
                Description = "Authentic Italian cuisine with fresh pasta and wood-fired pizzas",
                Cuisine = "Italian",
                Address = "123 Main Street",
                City = "Downtown",
                Phone = "+1234567894",
                PartnerUserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                IsOpen = true,
                IsApproved = true,
                Rating = 4.5,
                PrepTimeMinutes = 30,
                MinOrderAmount = 10m,
                DeliveryFee = 2.99m,
                CreatedAt = DateTime.UtcNow
            },
            new Restaurant
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Name = "Golden Dragon Chinese",
                Description = "Traditional Chinese dishes with modern flavors",
                Cuisine = "Chinese",
                Address = "456 Oak Avenue",
                City = "Chinatown",
                Phone = "+1234567895",
                PartnerUserId = Guid.Parse("33333333-3333-3333-3333-333333333334"),
                IsOpen = true,
                IsApproved = true,
                Rating = 4.3,
                PrepTimeMinutes = 25,
                MinOrderAmount = 15m,
                DeliveryFee = 3.99m,
                CreatedAt = DateTime.UtcNow
            },
            new Restaurant
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Name = "Spice of India",
                Description = "Aromatic Indian curries and tandoori specialties",
                Cuisine = "Indian",
                Address = "789 Curry Lane",
                City = "Little India",
                Phone = "+1234567896",
                PartnerUserId = Guid.Parse("33333333-3333-3333-3333-333333333335"),
                IsOpen = true,
                IsApproved = true,
                Rating = 4.7,
                PrepTimeMinutes = 35,
                MinOrderAmount = 12m,
                DeliveryFee = 2.49m,
                CreatedAt = DateTime.UtcNow
            },
            new Restaurant
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                Name = "Burger Palace",
                Description = "Gourmet burgers and crispy fries",
                Cuisine = "American",
                Address = "321 Burger Street",
                City = "Food District",
                Phone = "+1234567800",
                PartnerUserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                IsOpen = true,
                IsApproved = true,
                Rating = 4.2,
                PrepTimeMinutes = 20,
                MinOrderAmount = 8m,
                DeliveryFee = 1.99m,
                CreatedAt = DateTime.UtcNow
            },
            new Restaurant
            {
                Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                Name = "Sushi Master",
                Description = "Fresh sushi and Japanese delicacies",
                Cuisine = "Japanese",
                Address = "555 Sakura Boulevard",
                City = "Japan Town",
                Phone = "+1234567801",
                PartnerUserId = Guid.Parse("33333333-3333-3333-3333-333333333334"),
                IsOpen = false,
                IsApproved = true,
                Rating = 4.8,
                PrepTimeMinutes = 40,
                MinOrderAmount = 20m,
                DeliveryFee = 4.99m,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Restaurants.AddRange(restaurants);
        context.SaveChanges();

        // Create categories for each restaurant
        var categories = new List<Category>
        {
            // Mario's Italian Kitchen
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Pizza", DisplayOrder = 1, RestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111112"), Name = "Pasta", DisplayOrder = 2, RestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111113"), Name = "Dessert", DisplayOrder = 3, RestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
            new Category { Id = Guid.Parse("11111111-1111-1111-1111-111111111114"), Name = "Salad", DisplayOrder = 4, RestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") },
            
            // Golden Dragon Chinese
            new Category { Id = Guid.Parse("22222222-2222-2222-2222-222222222221"), Name = "Main Course", DisplayOrder = 1, RestaurantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
            new Category { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Rice", DisplayOrder = 2, RestaurantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
            new Category { Id = Guid.Parse("22222222-2222-2222-2222-222222222223"), Name = "Appetizer", DisplayOrder = 3, RestaurantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb") },
            
            // Spice of India
            new Category { Id = Guid.Parse("33333333-3333-3333-3333-333333333331"), Name = "Curry", DisplayOrder = 1, RestaurantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc") },
            new Category { Id = Guid.Parse("33333333-3333-3333-3333-333333333332"), Name = "Bread", DisplayOrder = 2, RestaurantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc") },
            new Category { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Rice", DisplayOrder = 3, RestaurantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc") },
            new Category { Id = Guid.Parse("33333333-3333-3333-3333-333333333334"), Name = "Appetizer", DisplayOrder = 4, RestaurantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc") },
            
            // Burger Palace
            new Category { Id = Guid.Parse("44444444-4444-4444-4444-444444444441"), Name = "Burger", DisplayOrder = 1, RestaurantId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd") },
            new Category { Id = Guid.Parse("44444444-4444-4444-4444-444444444442"), Name = "Sides", DisplayOrder = 2, RestaurantId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd") },
            new Category { Id = Guid.Parse("44444444-4444-4444-4444-444444444443"), Name = "Beverage", DisplayOrder = 3, RestaurantId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd") },
            
            // Sushi Master
            new Category { Id = Guid.Parse("55555555-5555-5555-5555-555555555551"), Name = "Sushi", DisplayOrder = 1, RestaurantId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
            new Category { Id = Guid.Parse("55555555-5555-5555-5555-555555555552"), Name = "Sashimi", DisplayOrder = 2, RestaurantId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
            new Category { Id = Guid.Parse("55555555-5555-5555-5555-555555555553"), Name = "Soup", DisplayOrder = 3, RestaurantId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") },
            new Category { Id = Guid.Parse("55555555-5555-5555-5555-555555555554"), Name = "Appetizer", DisplayOrder = 4, RestaurantId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") }
        };

        context.Categories.AddRange(categories);
        context.SaveChanges();

        var menuItems = new List<MenuItem>
        {
            // Mario's Italian Kitchen
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), CategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Margherita Pizza", Description = "Classic pizza with tomato, mozzarella, and basil", Price = 12.99m, IsVeg = true, IsAvailable = true, ImageUrl = "https://example.com/margherita.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), CategoryId = Guid.Parse("11111111-1111-1111-1111-111111111112"), Name = "Spaghetti Carbonara", Description = "Creamy pasta with bacon and parmesan", Price = 14.99m, IsVeg = false, IsAvailable = true, ImageUrl = "https://example.com/carbonara.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), CategoryId = Guid.Parse("11111111-1111-1111-1111-111111111113"), Name = "Tiramisu", Description = "Classic Italian dessert", Price = 6.99m, IsVeg = true, IsAvailable = true, ImageUrl = "https://example.com/tiramisu.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), CategoryId = Guid.Parse("11111111-1111-1111-1111-111111111114"), Name = "Caesar Salad", Description = "Fresh romaine lettuce", Price = 8.99m, IsVeg = true, IsAvailable = true, ImageUrl = "https://example.com/caesar.jpg" },

            // Golden Dragon Chinese
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), CategoryId = Guid.Parse("22222222-2222-2222-2222-222222222221"), Name = "Kung Pao Chicken", Description = "Spicy stir-fried chicken", Price = 13.99m, IsVeg = false, IsAvailable = true, ImageUrl = "https://example.com/kungpao.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), CategoryId = Guid.Parse("22222222-2222-2222-2222-222222222221"), Name = "Sweet and Sour Pork", Description = "Crispy pork", Price = 12.99m, IsVeg = false, IsAvailable = true, ImageUrl = "https://example.com/sweetsour.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), CategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Fried Rice", Description = "Classic fried rice", Price = 9.99m, IsVeg = true, IsAvailable = true, ImageUrl = "https://example.com/friedrice.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), CategoryId = Guid.Parse("22222222-2222-2222-2222-222222222223"), Name = "Spring Rolls", Description = "Crispy vegetable rolls", Price = 5.99m, IsVeg = true, IsAvailable = true, ImageUrl = "https://example.com/springrolls.jpg" },

            // Spice of India
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), CategoryId = Guid.Parse("33333333-3333-3333-3333-333333333331"), Name = "Chicken Tikka Masala", Description = "Tender chicken in creamy sauce", Price = 15.99m, IsVeg = false, IsAvailable = true, ImageUrl = "https://example.com/tikka.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), CategoryId = Guid.Parse("33333333-3333-3333-3333-333333333332"), Name = "Butter Naan", Description = "Soft Indian bread", Price = 3.99m, IsVeg = true, IsAvailable = true, ImageUrl = "https://example.com/naan.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), CategoryId = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Biryani", Description = "Aromatic rice with spices", Price = 14.99m, IsVeg = false, IsAvailable = true, ImageUrl = "https://example.com/biryani.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), CategoryId = Guid.Parse("33333333-3333-3333-3333-333333333334"), Name = "Samosa", Description = "Crispy pastry", Price = 4.99m, IsVeg = true, IsAvailable = true, ImageUrl = "https://example.com/samosa.jpg" },

            // Burger Palace
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), CategoryId = Guid.Parse("44444444-4444-4444-4444-444444444441"), Name = "Classic Cheeseburger", Description = "Beef patty with cheese", Price = 10.99m, IsVeg = false, IsAvailable = true, ImageUrl = "https://example.com/cheeseburger.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), CategoryId = Guid.Parse("44444444-4444-4444-4444-444444444441"), Name = "Bacon Burger", Description = "Beef patty with bacon", Price = 12.99m, IsVeg = false, IsAvailable = true, ImageUrl = "https://example.com/baconburger.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), CategoryId = Guid.Parse("44444444-4444-4444-4444-444444444442"), Name = "French Fries", Description = "Crispy golden fries", Price = 4.99m, IsVeg = true, IsAvailable = true, ImageUrl = "https://example.com/fries.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), CategoryId = Guid.Parse("44444444-4444-4444-4444-444444444443"), Name = "Milkshake", Description = "Creamy vanilla milkshake", Price = 5.99m, IsVeg = true, IsAvailable = true, ImageUrl = "https://example.com/milkshake.jpg" },

            // Sushi Master
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), CategoryId = Guid.Parse("55555555-5555-5555-5555-555555555551"), Name = "California Roll", Description = "Crab, avocado, cucumber", Price = 11.99m, IsVeg = false, IsAvailable = true, ImageUrl = "https://example.com/california.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), CategoryId = Guid.Parse("55555555-5555-5555-5555-555555555552"), Name = "Salmon Sashimi", Description = "Fresh sliced salmon", Price = 16.99m, IsVeg = false, IsAvailable = true, ImageUrl = "https://example.com/sashimi.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), CategoryId = Guid.Parse("55555555-5555-5555-5555-555555555553"), Name = "Miso Soup", Description = "Traditional Japanese soup", Price = 3.99m, IsVeg = true, IsAvailable = true, ImageUrl = "https://example.com/miso.jpg" },
            new MenuItem { Id = Guid.NewGuid(), RestaurantId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), CategoryId = Guid.Parse("55555555-5555-5555-5555-555555555554"), Name = "Tempura", Description = "Lightly battered vegetables", Price = 9.99m, IsVeg = true, IsAvailable = true, ImageUrl = "https://example.com/tempura.jpg" }
        };

        context.MenuItems.AddRange(menuItems);
        context.SaveChanges();

        Console.WriteLine("✅ Catalog Service: Seeded 5 restaurants, 18 categories, and 24 menu items");
    }
}
