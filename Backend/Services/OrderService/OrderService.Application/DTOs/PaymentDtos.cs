using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace OrderService.Application.DTOs;

public class SimulatePaymentDto
{
    [Required] public Guid OrderId { get; set; }
    [Required] public string Method { get; set; } = string.Empty; // COD | Card | Wallet
    /// <summary>true = success, false = failure (for simulation/testing)</summary>
    public bool ShouldSucceed { get; set; } = true;
}

public class PaymentResultDto
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Success | Failed
    public string? TransactionId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class PaymentSummaryDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
}
