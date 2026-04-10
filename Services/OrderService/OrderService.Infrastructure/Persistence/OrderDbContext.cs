using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<DeliveryAssignment> DeliveryAssignments => Set<DeliveryAssignment>();
    public DbSet<DeliveryStatusHistory> DeliveryStatusHistory => Set<DeliveryStatusHistory>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Subtotal).HasColumnType("decimal(10,2)");
            e.Property(o => o.DeliveryFee).HasColumnType("decimal(10,2)");
            e.Property(o => o.Discount).HasColumnType("decimal(10,2)");
            e.Property(o => o.GstAmount).HasColumnType("decimal(10,2)");
            e.Property(o => o.TotalAmount).HasColumnType("decimal(10,2)");
            e.Property(o => o.Status).HasConversion<int>();
            e.Property(o => o.RestaurantName).HasMaxLength(200);
            e.Property(o => o.DeliveryAddress).IsRequired().HasMaxLength(500);
            e.Property(o => o.PaymentMethod).IsRequired().HasMaxLength(20);

            e.HasMany(o => o.Items)
             .WithOne(i => i.Order)
             .HasForeignKey(i => i.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(o => o.Payment)
             .WithOne(p => p.Order)
             .HasForeignKey<Payment>(p => p.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(o => o.DeliveryAssignment)
             .WithOne(d => d.Order)
             .HasForeignKey<DeliveryAssignment>(d => d.OrderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(o => o.CustomerId);
            e.HasIndex(o => o.RestaurantId);
        });

        b.Entity<OrderItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.UnitPrice).HasColumnType("decimal(10,2)");
            e.Property(i => i.Name).IsRequired().HasMaxLength(200);
        });

        b.Entity<Cart>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.CustomerId).IsUnique();
            e.Property(c => c.Discount).HasColumnType("decimal(10,2)");

            e.HasMany(c => c.Items)
             .WithOne(i => i.Cart)
             .HasForeignKey(i => i.CartId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CartItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.UnitPrice).HasColumnType("decimal(10,2)");
            e.Property(i => i.Name).IsRequired().HasMaxLength(200);
        });

        b.Entity<Payment>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Amount).HasColumnType("decimal(10,2)");
            e.Property(p => p.Method).IsRequired().HasMaxLength(20);
            e.Property(p => p.Status).HasConversion<int>();
            e.HasIndex(p => p.OrderId).IsUnique();
        });

        b.Entity<DeliveryAssignment>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Status).HasConversion<int>();
            e.Property(d => d.AgentName).IsRequired().HasMaxLength(150);
            e.Property(d => d.AgentMobile).HasMaxLength(15);
            e.HasIndex(d => d.OrderId).IsUnique();
            e.HasIndex(d => d.AgentId);

            e.HasMany(d => d.StatusHistory)
             .WithOne(h => h.DeliveryAssignment)
             .HasForeignKey(h => h.DeliveryAssignmentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<DeliveryStatusHistory>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Status).HasConversion<int>();
        });
    }
}
