using AdminService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using System.Reflection.Emit;

namespace AdminService.Infrastructure.Persistence;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<UserSnapshot> UserSnapshots => Set<UserSnapshot>();
    public DbSet<OrderSnapshot> OrderSnapshots => Set<OrderSnapshot>();
    public DbSet<AdminAuditLog> AuditLogs => Set<AdminAuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<UserSnapshot>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.Mobile).HasMaxLength(15);
            e.Property(u => u.Role).IsRequired().HasMaxLength(20);
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Role);
        });

        b.Entity<OrderSnapshot>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.TotalAmount).HasColumnType("decimal(10,2)");
            e.Property(o => o.Status).HasMaxLength(30);
            e.Property(o => o.PaymentMethod).HasMaxLength(20);
            e.Property(o => o.RestaurantName).HasMaxLength(200);
            e.Property(o => o.CustomerEmail).HasMaxLength(256);
            e.HasIndex(o => o.CustomerId);
            e.HasIndex(o => o.RestaurantId);
            e.HasIndex(o => o.Status);
            e.HasIndex(o => o.PlacedAt);
        });

        b.Entity<AdminAuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Action).IsRequired().HasMaxLength(100);
            e.Property(a => a.EntityType).IsRequired().HasMaxLength(50);
            e.HasIndex(a => new { a.EntityType, a.EntityId });
            e.HasIndex(a => a.AdminUserId);
        });
    }
}