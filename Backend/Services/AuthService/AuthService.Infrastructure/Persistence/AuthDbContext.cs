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
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

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
            e.Property(u => u.ProfileImageUrl).HasMaxLength(500);
            e.Property(u => u.WalletBalance).HasColumnType("decimal(10,2)").HasDefaultValue(0);

            // ── Delivery Agent fields ─────────────────────────
            e.Property(u => u.VehicleType).HasMaxLength(20);
            e.Property(u => u.VehicleNumber).HasMaxLength(20);

            // ── Soft Delete fields ─────────────────────────
            e.Property(u => u.DeletionReason).HasMaxLength(500);
            e.HasIndex(u => u.IsDeleted);

            // ── Approval fields ─────────────────────────────
            e.Property(u => u.ApprovalNotes).HasMaxLength(500);
            e.Property(u => u.RejectionReason).HasMaxLength(500);
            e.HasIndex(u => new { u.Role, u.IsApproved });
        });

        builder.Entity<Address>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.UserId);
            e.Property(a => a.Label).IsRequired().HasMaxLength(50);
            e.Property(a => a.FullAddress).IsRequired().HasMaxLength(500);
            e.Property(a => a.City).IsRequired().HasMaxLength(100);
            e.Property(a => a.State).IsRequired().HasMaxLength(100);
            e.Property(a => a.Pincode).IsRequired().HasMaxLength(10);
            e.Property(a => a.Landmark).HasMaxLength(200);

            e.HasOne(a => a.User)
             .WithMany()
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WalletTransaction>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasIndex(w => w.UserId);
            e.HasIndex(w => w.CreatedAt);
            e.Property(w => w.Amount).HasColumnType("decimal(10,2)").IsRequired();
            e.Property(w => w.Description).IsRequired().HasMaxLength(500);

            e.HasOne(w => w.User)
             .WithMany()
             .HasForeignKey(w => w.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}