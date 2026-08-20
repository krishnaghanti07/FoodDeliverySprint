using AdminService.Domain.Entities;
using AdminService.Infrastructure.Persistence;

namespace AdminService.Infrastructure.Data;

public static class DataSeeder
{
    public static void SeedData(AdminDbContext context)
    {
        // Check if data already exists
        if (context.UserSnapshots.Any())
        {
            Console.WriteLine("✅ Admin data already seeded");
            return;
        }

        var userSnapshots = new List<UserSnapshot>
        {
            // Admin User
            new UserSnapshot
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                FullName = "System Administrator",
                Email = "admin@fooddelivery.com",
                Mobile = "+1234567890",
                Role = "Admin",
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            },
            // Customer Users
            new UserSnapshot
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                FullName = "John Doe",
                Email = "john.doe@example.com",
                Mobile = "+1234567891",
                Role = "Customer",
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            },
            new UserSnapshot
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222223"),
                FullName = "Jane Smith",
                Email = "jane.smith@example.com",
                Mobile = "+1234567892",
                Role = "Customer",
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            },
            new UserSnapshot
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222224"),
                FullName = "Mike Johnson",
                Email = "mike.johnson@example.com",
                Mobile = "+1234567893",
                Role = "Customer",
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            },
            // Partner Users
            new UserSnapshot
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                FullName = "Mario Rossi",
                Email = "mario@italianrestaurant.com",
                Mobile = "+1234567894",
                Role = "Partner",
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            },
            new UserSnapshot
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333334"),
                FullName = "Chen Wei",
                Email = "chen@chineserestaurant.com",
                Mobile = "+1234567895",
                Role = "Partner",
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            },
            new UserSnapshot
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333335"),
                FullName = "Raj Patel",
                Email = "raj@indianrestaurant.com",
                Mobile = "+1234567896",
                Role = "Partner",
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            },
            // Delivery Agents
            new UserSnapshot
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                FullName = "David Wilson",
                Email = "david.delivery@fooddelivery.com",
                Mobile = "+1234567897",
                Role = "DeliveryAgent",
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            },
            new UserSnapshot
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444445"),
                FullName = "Sarah Brown",
                Email = "sarah.delivery@fooddelivery.com",
                Mobile = "+1234567898",
                Role = "DeliveryAgent",
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            },
            new UserSnapshot
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444446"),
                FullName = "Tom Anderson",
                Email = "tom.delivery@fooddelivery.com",
                Mobile = "+1234567899",
                Role = "DeliveryAgent",
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            }
        };

        context.UserSnapshots.AddRange(userSnapshots);
        context.SaveChanges();

        Console.WriteLine("✅ Admin Service: Seeded 10 user snapshots");
    }
}
