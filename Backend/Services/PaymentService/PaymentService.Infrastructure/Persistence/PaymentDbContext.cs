using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using PaymentService.Domain.Entities;
using System.Reflection.Emit;

namespace PaymentService.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<PaymentTransaction> Transactions => Set<PaymentTransaction>();
    public DbSet<RazorpayOrder> RazorpayOrders => Set<RazorpayOrder>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<PaymentTransaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasColumnType("decimal(12,2)").IsRequired();
            e.Property(t => t.RefundAmount).HasColumnType("decimal(12,2)");
            e.Property(t => t.Currency).HasMaxLength(5);
            e.Property(t => t.Method).IsRequired().HasMaxLength(20);
            e.Property(t => t.Status).HasConversion<int>();
            e.Property(t => t.Gateway).HasConversion<int>();
            e.Property(t => t.GatewayTxnId).HasMaxLength(100);
            e.Property(t => t.GatewayOrderId).HasMaxLength(100);
            e.Property(t => t.FailureReason).HasMaxLength(500);
            e.Property(t => t.RefundReason).HasMaxLength(500);

            e.HasIndex(t => t.OrderId).IsUnique();      // one payment per order
            e.HasIndex(t => t.CustomerId);
            e.HasIndex(t => t.Status);
            e.HasIndex(t => t.CreatedAt);
        });

        b.Entity<RazorpayOrder>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Amount).HasColumnType("decimal(12,2)");
            e.Property(r => r.RazorpayOrderId).IsRequired().HasMaxLength(100);
            e.Property(r => r.Currency).HasMaxLength(5);
            e.Property(r => r.Status).HasMaxLength(20);
            e.HasIndex(r => r.OrderId).IsUnique();
            e.HasIndex(r => r.RazorpayOrderId);
        });
    }
}