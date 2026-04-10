using System;
using System.Collections.Generic;
using System.Text;
using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.Mobile).IsRequired().HasMaxLength(15);
            e.Property(u => u.Role).IsRequired().HasMaxLength(20);

            // ── Delivery Agent fields ─────────────────────────
            e.Property(u => u.VehicleType).HasMaxLength(20);    // ← NEW
            e.Property(u => u.VehicleNumber).HasMaxLength(20);  // ← NEW
        });
    }
}