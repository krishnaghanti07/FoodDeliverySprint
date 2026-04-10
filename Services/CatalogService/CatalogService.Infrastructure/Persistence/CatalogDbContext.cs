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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Restaurant>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Name).IsRequired().HasMaxLength(200);
            e.Property(r => r.Cuisine).IsRequired().HasMaxLength(100);
            e.Property(r => r.DeliveryFee).HasColumnType("decimal(10,2)");
            e.Property(r => r.MinOrderAmount).HasColumnType("decimal(10,2)");
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
        });
    }
}