using AuthService.Domain.Entities;
using AuthService.Infrastructure.Persistence;

namespace AuthService.Infrastructure.Data;

public static class DataSeeder
{
    public static void SeedData(AuthDbContext context)
    {
        // Check if data already exists
        if (context.Users.Any())
        {
            Console.WriteLine("✅ Auth data already seeded");
            return;
        }

        var users = new List<User>
        {
            // Admin User
            new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                FullName = "System Administrator",
                Email = "admin@fooddelivery.com",
                Mobile = "+1234567890",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
                Role = "Admin",
                IsActive = true,
                IsEmailVerified = true,
                TwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow
            },
            // Customer Users
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                FullName = "John Doe",
                Email = "john.doe@example.com",
                Mobile = "+1234567891",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                Role = "Customer",
                IsActive = true,
                IsEmailVerified = true,
                TwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222223"),
                FullName = "Jane Smith",
                Email = "jane.smith@example.com",
                Mobile = "+1234567892",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                Role = "Customer",
                IsActive = true,
                IsEmailVerified = true,
                TwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222224"),
                FullName = "Mike Johnson",
                Email = "mike.johnson@example.com",
                Mobile = "+1234567893",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                Role = "Customer",
                IsActive = true,
                IsEmailVerified = true,
                TwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow
            },
            // Partner Users (Restaurant Owners)
            new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                FullName = "Mario Rossi",
                Email = "mario@italianrestaurant.com",
                Mobile = "+1234567894",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Partner@123"),
                Role = "Partner",
                IsActive = true,
                IsEmailVerified = true,
                TwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333334"),
                FullName = "Chen Wei",
                Email = "chen@chineserestaurant.com",
                Mobile = "+1234567895",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Partner@123"),
                Role = "Partner",
                IsActive = true,
                IsEmailVerified = true,
                TwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333335"),
                FullName = "Raj Patel",
                Email = "raj@indianrestaurant.com",
                Mobile = "+1234567896",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Partner@123"),
                Role = "Partner",
                IsActive = true,
                IsEmailVerified = true,
                TwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow
            },
            // Delivery Agents
            new User
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                FullName = "David Wilson",
                Email = "david.delivery@fooddelivery.com",
                Mobile = "+1234567897",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Delivery@123"),
                Role = "DeliveryAgent",
                IsActive = true,
                IsEmailVerified = true,
                TwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444445"),
                FullName = "Sarah Brown",
                Email = "sarah.delivery@fooddelivery.com",
                Mobile = "+1234567898",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Delivery@123"),
                Role = "DeliveryAgent",
                IsActive = true,
                IsEmailVerified = true,
                TwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444446"),
                FullName = "Tom Anderson",
                Email = "tom.delivery@fooddelivery.com",
                Mobile = "+1234567899",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Delivery@123"),
                Role = "DeliveryAgent",
                IsActive = true,
                IsEmailVerified = true,
                TwoFactorEnabled = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Users.AddRange(users);
        context.SaveChanges();

        Console.WriteLine("✅ Auth Service: Seeded 10 users (1 Admin, 3 Customers, 3 Partners, 3 Delivery Agents)");
        Console.WriteLine("   📧 Admin: admin@fooddelivery.com / Admin@1234");
        Console.WriteLine("   📧 Customer: john.doe@example.com / Customer@123");
        Console.WriteLine("   📧 Partner: mario@italianrestaurant.com / Partner@123");
        Console.WriteLine("   📧 Delivery: david.delivery@fooddelivery.com / Delivery@123");
    }
}
