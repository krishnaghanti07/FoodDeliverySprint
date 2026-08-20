using System;
using System.Collections.Generic;
using System.Text;
namespace OrderService.Domain.Entities;

public class DeliveryAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string AgentMobile { get; set; } = string.Empty;
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Assigned;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PickedUpAt { get; set; }
    public DateTime? OutForDeliveryAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveryNotes { get; set; }
    public string? FailureReason { get; set; }
    
    // New fields for time estimates
    public DateTime? EstimatedArrivalTime { get; set; }
    public DateTime? ActualArrivalTime { get; set; }
    
    // New fields for self-service assignment
    public bool IsAutoAssigned { get; set; } = false; // true = admin assigned, false = agent accepted
    public DateTime? AcceptedAt { get; set; } // When agent accepted the order
    
    public ICollection<DeliveryStatusHistory> StatusHistory { get; set; } = new List<DeliveryStatusHistory>();
}

