using System;

namespace OrderService.Domain.Entities;

public class RefundRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public decimal OriginalAmount { get; set; }  // Total order amount
    public decimal PlatformFee { get; set; }     // Platform fee to deduct
    public decimal CancellationCharge { get; set; } // Cancellation charge
    public decimal RefundAmount { get; set; }    // Actual refund = Original - PlatformFee - CancellationCharge
    public RefundStatus Status { get; set; } = RefundStatus.PendingApproval;
    public string? AdminNotes { get; set; }
    public Guid? ProcessedBy { get; set; }       // Admin who processed
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}

public enum RefundStatus
{
    PendingApproval = 0,  // Waiting for admin approval
    Approved = 1,          // Admin approved, ready to refund
    Rejected = 2,          // Admin rejected the refund
    Completed = 3          // Refund completed and added to wallet
}
