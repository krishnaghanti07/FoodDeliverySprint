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
    public DbSet<RestaurantSnapshot> RestaurantSnapshots => Set<RestaurantSnapshot>();
    public DbSet<DeliveryAgentSnapshot> DeliveryAgentSnapshots => Set<DeliveryAgentSnapshot>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<NotificationHistory> NotificationHistory => Set<NotificationHistory>();

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

        b.Entity<RestaurantSnapshot>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Name).IsRequired().HasMaxLength(200);
            e.Property(r => r.Description).HasMaxLength(1000);
            e.Property(r => r.Address).HasMaxLength(500);
            e.Property(r => r.Phone).HasMaxLength(20);
            e.Property(r => r.PartnerName).HasMaxLength(150);
            e.Property(r => r.Status).IsRequired().HasMaxLength(20);
            e.Property(r => r.AverageRating).HasColumnType("decimal(3,2)");
            e.Property(r => r.TotalRevenue).HasColumnType("decimal(12,2)");
            e.HasIndex(r => r.Status);
            e.HasIndex(r => r.PartnerId);
        });

        b.Entity<DeliveryAgentSnapshot>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.FullName).IsRequired().HasMaxLength(150);
            e.Property(a => a.Email).IsRequired().HasMaxLength(256);
            e.Property(a => a.Mobile).HasMaxLength(15);
            e.Property(a => a.VehicleType).HasMaxLength(50);
            e.Property(a => a.AverageRating).HasColumnType("decimal(3,2)");
            
            // ── Approval fields ─────────────────────────────
            e.Property(a => a.ApprovalNotes).HasMaxLength(500);
            e.HasIndex(a => a.IsApproved);
            
            e.HasIndex(a => a.Email).IsUnique();
            e.HasIndex(a => a.IsActive);
            e.HasIndex(a => a.IsOnline);
        });

        b.Entity<Complaint>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.CustomerEmail).IsRequired().HasMaxLength(256);
            e.Property(c => c.Type).IsRequired().HasMaxLength(50);
            e.Property(c => c.Subject).IsRequired().HasMaxLength(200);
            e.Property(c => c.Description).IsRequired().HasMaxLength(2000);
            e.Property(c => c.Status).IsRequired().HasMaxLength(20);
            e.Property(c => c.Resolution).HasMaxLength(2000);
            e.HasIndex(c => c.CustomerId);
            e.HasIndex(c => c.Status);
            e.HasIndex(c => c.Type);
            e.HasIndex(c => c.CreatedAt);
        });

        b.Entity<NotificationHistory>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Recipients).IsRequired().HasMaxLength(500);
            e.Property(n => n.Title).IsRequired().HasMaxLength(200);
            e.Property(n => n.Message).IsRequired().HasMaxLength(2000);
            e.Property(n => n.Type).IsRequired().HasMaxLength(20);
            e.HasIndex(n => n.SentBy);
            e.HasIndex(n => n.SentAt);
            e.HasIndex(n => n.Type);
        });
    }
}