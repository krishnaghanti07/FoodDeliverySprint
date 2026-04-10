using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.DTOs;

public class DeliveryAssignmentDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid AgentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string AgentMobile { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? OutForDeliveryAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? DeliveryNotes { get; set; }
    public List<DeliveryHistoryDto> StatusHistory { get; set; } = new();
}

public class DeliveryHistoryDto
{
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; }
}

public class AssignDeliveryAgentDto
{
    [Required] public Guid OrderId { get; set; }
    [Required] public Guid AgentId { get; set; }
    [Required] public string AgentName { get; set; } = string.Empty;
    [Required] public string AgentMobile { get; set; } = string.Empty;
}

public class UpdateDeliveryStatusDto
{
    /// <summary>PickedUp | OutForDelivery | Delivered | Failed</summary>
    [Required] public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}
