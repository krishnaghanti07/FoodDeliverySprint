using System;
using System.Collections.Generic;
using System.Text;
using CatalogService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Persistence;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<OperatingHour> OperatingHours => Set<OperatingHour>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Restaurant>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Name).IsRequired().HasMaxLength(200);
            e.Property(r => r.Cuisine).IsRequired().HasMaxLength(100);
            e.Property(r => r.DeliveryFee).HasColumnType("decimal(10,2)");
            e.Property(r => r.MinOrderAmount).HasColumnType("decimal(10,2)");
            
            // ── Soft Delete fields ─────────────────────────
            e.Property(r => r.DeletionReason).HasMaxLength(500);
            e.HasIndex(r => r.IsDeleted);
            
            e.HasMany(r => r.Categories).WithOne(c => c.Restaurant)
             .HasForeignKey(c => c.RestaurantId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired().HasMaxLength(100);
            e.HasMany(c => c.MenuItems).WithOne(m => m.Category)
             .HasForeignKey(m => m.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MenuItem>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Name).IsRequired().HasMaxLength(200);
            e.Property(m => m.Price).HasColumnType("decimal(10,2)");
            e.Property(m => m.DietaryTags).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
        });

        builder.Entity<OperatingHour>(e =>
        {
            e.HasKey(oh => oh.Id);
            e.HasOne(oh => oh.Restaurant).WithMany(r => r.OperatingHours)
             .HasForeignKey(oh => oh.RestaurantId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Review>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.UserName).IsRequired().HasMaxLength(150);
            e.Property(r => r.Comment).HasMaxLength(1000);
            e.HasOne(r => r.Restaurant).WithMany(rest => rest.Reviews)
             .HasForeignKey(r => r.RestaurantId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => new { r.UserId, r.RestaurantId }).IsUnique();
        });
    }
}