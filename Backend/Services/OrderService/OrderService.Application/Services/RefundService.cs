using Microsoft.Extensions.Logging;
using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Services;

public class RefundService : IRefundService
{
    private readonly IRefundRepository _refundRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly ILogger<RefundService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public RefundService(
        IRefundRepository refundRepo, 
        IOrderRepository orderRepo,
        ILogger<RefundService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _refundRepo = refundRepo;
        _orderRepo = orderRepo;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<RefundRequestDto>> GetPendingRefundsAsync()
    {
        var refunds = await _refundRepo.GetPendingRefundsAsync();
        return refunds.Select(MapToDto).ToList();
    }

    public async Task<List<RefundRequestDto>> GetAllRefundsAsync(string? statusFilter = null)
    {
        List<RefundRequest> refunds;

        if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<RefundStatus>(statusFilter, true, out var status))
        {
            refunds = await _refundRepo.GetByStatusAsync(status);
        }
        else
        {
            // Get all refunds by fetching each status
            var pending = await _refundRepo.GetByStatusAsync(RefundStatus.PendingApproval);
            var approved = await _refundRepo.GetByStatusAsync(RefundStatus.Approved);
            var rejected = await _refundRepo.GetByStatusAsync(RefundStatus.Rejected);
            var completed = await _refundRepo.GetByStatusAsync(RefundStatus.Completed);
            
            refunds = pending.Concat(approved).Concat(rejected).Concat(completed)
                .OrderByDescending(r => r.RequestedAt)
                .ToList();
        }

        return refunds.Select(MapToDto).ToList();
    }

    public async Task<RefundRequestDto?> GetRefundByIdAsync(Guid refundId)
    {
        var refund = await _refundRepo.GetByIdAsync(refundId);
        return refund == null ? null : MapToDto(refund);
    }

    public async Task<RefundRequestDto?> GetRefundByOrderIdAsync(Guid orderId)
    {
        var refund = await _refundRepo.GetByOrderIdAsync(orderId);
        return refund == null ? null : MapToDto(refund);
    }

    public async Task<RefundRequestDto> ProcessRefundAsync(Guid refundId, string action, string? adminNotes, Guid processedBy)
    {
        var refund = await _refundRepo.GetByIdAsync(refundId)
            ?? throw new KeyNotFoundException("Refund request not found.");

        if (refund.Status != RefundStatus.PendingApproval)
            throw new InvalidOperationException($"Refund request is already {refund.Status}. Only pending refunds can be processed.");

        var actionLower = action.ToLower();
        
        if (actionLower == "approve")
        {
            refund.Status = RefundStatus.Approved;
            refund.ProcessedAt = DateTime.UtcNow;
            refund.ProcessedBy = processedBy;
            refund.AdminNotes = adminNotes;
        }
        else if (actionLower == "reject")
        {
            refund.Status = RefundStatus.Rejected;
            refund.ProcessedAt = DateTime.UtcNow;
            refund.ProcessedBy = processedBy;
            refund.AdminNotes = adminNotes;
        }
        else
        {
            throw new ArgumentException("Action must be 'Approve' or 'Reject'.");
        }

        await _refundRepo.UpdateAsync(refund);
        await _refundRepo.SaveChangesAsync();

        return MapToDto(refund);
    }

    public async Task<RefundRequestDto> ApproveRefundForOrderAsync(
        Guid orderId, 
        Guid customerId, 
        decimal originalAmount, 
        decimal platformFee, 
        decimal cancellationCharge, 
        decimal refundAmount, 
        string? adminNotes)
    {
        // Get the order
        var order = await _orderRepo.GetByIdAsync(orderId)
            ?? throw new KeyNotFoundException($"Order {orderId} not found.");

        // Verify order is cancelled
        if (order.Status != OrderStatus.Cancelled)
            throw new InvalidOperationException("Only cancelled orders can be refunded.");

        // Verify payment method is not COD
        if (order.PaymentMethod.Equals("COD", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("COD orders do not require refund approval.");

        // Check if refund request already exists
        var existingRefund = await _refundRepo.GetByOrderIdAsync(orderId);
        
        RefundRequest refund;
        if (existingRefund != null)
        {
            // Update existing refund
            refund = existingRefund;
            refund.Status = RefundStatus.Approved;
            refund.ProcessedAt = DateTime.UtcNow;
            refund.AdminNotes = adminNotes;
            await _refundRepo.UpdateAsync(refund);
        }
        else
        {
            // Create new refund request
            refund = new RefundRequest
            {
                OrderId = orderId,
                CustomerId = customerId,
                OriginalAmount = originalAmount,
                PlatformFee = platformFee,
                CancellationCharge = cancellationCharge,
                RefundAmount = refundAmount,
                Status = RefundStatus.Approved,
                RequestedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                AdminNotes = adminNotes
            };
            await _refundRepo.AddAsync(refund);
        }

        await _refundRepo.SaveChangesAsync();

        // Credit wallet via AuthService
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var walletRequest = new
            {
                userId = customerId,
                amount = refundAmount,
                description = $"Refund for cancelled order {orderId.ToString().Substring(0, 8)}"
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(walletRequest),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync("http://localhost:5001/api/auth/wallet/add", content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to credit wallet for customer {CustomerId}. Status: {Status}", 
                    customerId, response.StatusCode);
                throw new InvalidOperationException("Failed to credit wallet. Please try again.");
            }

            _logger.LogInformation("Wallet credited successfully for customer {CustomerId}, amount {Amount}", 
                customerId, refundAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crediting wallet for customer {CustomerId}", customerId);
            throw new InvalidOperationException("Failed to credit wallet. Please contact support.");
        }

        // Update order status to Refunded
        order.Status = OrderStatus.Refunded;
        order.UpdatedAt = DateTime.UtcNow;
        await _orderRepo.UpdateAsync(order);
        await _orderRepo.SaveChangesAsync();

        _logger.LogInformation("Refund approved for order {OrderId}, amount {Amount} credited to customer {CustomerId}", 
            orderId, refundAmount, customerId);

        return MapToDto(refund);
    }

    public async Task RejectRefundForOrderAsync(Guid orderId, string? adminNotes)
    {
        // Get the order
        var order = await _orderRepo.GetByIdAsync(orderId)
            ?? throw new KeyNotFoundException($"Order {orderId} not found.");

        // Verify order is cancelled
        if (order.Status != OrderStatus.Cancelled)
            throw new InvalidOperationException("Only cancelled orders can have refund requests.");

        // Check if refund request already exists
        var existingRefund = await _refundRepo.GetByOrderIdAsync(orderId);
        
        RefundRequest refund;
        if (existingRefund != null)
        {
            // Update existing refund
            refund = existingRefund;
            refund.Status = RefundStatus.Rejected;
            refund.ProcessedAt = DateTime.UtcNow;
            refund.AdminNotes = adminNotes;
            await _refundRepo.UpdateAsync(refund);
        }
        else
        {
            // Create new refund request as rejected
            refund = new RefundRequest
            {
                OrderId = orderId,
                CustomerId = order.CustomerId,
                OriginalAmount = order.TotalAmount,
                PlatformFee = order.PlatformFee,
                CancellationCharge = order.TotalAmount * 0.05m,
                RefundAmount = order.TotalAmount - order.PlatformFee - (order.TotalAmount * 0.05m),
                Status = RefundStatus.Rejected,
                RequestedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                AdminNotes = adminNotes
            };
            await _refundRepo.AddAsync(refund);
        }

        await _refundRepo.SaveChangesAsync();

        // Update order status to RefundRejected
        // Platform keeps: Platform Fee + Cancellation Charge
        order.Status = OrderStatus.RefundRejected;
        order.UpdatedAt = DateTime.UtcNow;
        await _orderRepo.UpdateAsync(order);
        await _orderRepo.SaveChangesAsync();

        _logger.LogInformation("Refund rejected for order {OrderId}. Platform keeps Platform Fee (₹{PlatformFee}) + Cancellation Charge (₹{CancellationCharge})", 
            orderId, order.PlatformFee, order.TotalAmount * 0.05m);
    }

    private static RefundRequestDto MapToDto(RefundRequest r) => new()
    {
        Id = r.Id,
        OrderId = r.OrderId,
        CustomerId = r.CustomerId,
        OriginalAmount = r.OriginalAmount,
        PlatformFee = r.PlatformFee,
        CancellationCharge = r.CancellationCharge,
        RefundAmount = r.RefundAmount,
        Status = r.Status.ToString(),
        AdminNotes = r.AdminNotes,
        ProcessedBy = r.ProcessedBy,
        RequestedAt = r.RequestedAt,
        ProcessedAt = r.ProcessedAt,
        RefundedAt = r.RefundedAt,
        // Order details if available
        OrderNumber = r.Order?.Id.ToString(),
        RestaurantName = r.Order?.RestaurantName
    };
}
