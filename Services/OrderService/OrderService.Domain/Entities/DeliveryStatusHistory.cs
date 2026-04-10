using System;
using System.Collections.Generic;
using System.Text;

namespace OrderService.Domain.Entities
{
    public class DeliveryStatusHistory
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DeliveryAssignmentId { get; set; }
        public DeliveryAssignment DeliveryAssignment { get; set; } = null!;
        public DeliveryStatus Status { get; set; }
        public string? Note { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }

    public enum DeliveryStatus
    {
        Assigned = 0,
        PickedUp = 1,
        OutForDelivery = 2,
        Delivered = 3,
        Failed = 4
    }
}
