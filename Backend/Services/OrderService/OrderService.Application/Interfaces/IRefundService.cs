using OrderService.Application.DTOs;

namespace OrderService.Application.Interfaces;

public interface IRefundService
{
    Task<List<RefundRequestDto>> GetPendingRefundsAsync();
    Task<List<RefundRequestDto>> GetAllRefundsAsync(string? statusFilter = null);
    Task<RefundRequestDto?> GetRefundByIdAsync(Guid refundId);
    Task<RefundRequestDto?> GetRefundByOrderIdAsync(Guid orderId);
    Task<RefundRequestDto> ProcessRefundAsync(Guid refundId, string action, string? adminNotes, Guid processedBy);
    Task<RefundRequestDto> ApproveRefundForOrderAsync(Guid orderId, Guid customerId, decimal originalAmount, decimal platformFee, decimal cancellationCharge, decimal refundAmount, string? adminNotes);
    Task RejectRefundForOrderAsync(Guid orderId, string? adminNotes);
}
