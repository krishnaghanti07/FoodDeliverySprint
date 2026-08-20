namespace OrderService.Application.Services;

public static class RefundCalculator
{
    private const decimal CANCELLATION_CHARGE_PERCENTAGE = 0.05m; // 5% cancellation charge
    
    /// <summary>
    /// Calculates refund amount after deducting platform fee and cancellation charges
    /// </summary>
    /// <param name="originalAmount">Total order amount paid by customer</param>
    /// <param name="platformFee">Platform fee (Rs. 15)</param>
    /// <returns>Tuple of (refundAmount, platformFee, cancellationCharge)</returns>
    public static (decimal refundAmount, decimal platformFee, decimal cancellationCharge) CalculateRefund(
        decimal originalAmount,
        decimal platformFee)
    {
        // Calculate cancellation charge (5% of original amount)
        var cancellationCharge = Math.Round(originalAmount * CANCELLATION_CHARGE_PERCENTAGE, 2);
        
        // Refund = Original - Platform Fee - Cancellation Charge
        var refundAmount = originalAmount - platformFee - cancellationCharge;
        
        // Ensure refund is not negative
        if (refundAmount < 0) refundAmount = 0;
        
        return (refundAmount, platformFee, cancellationCharge);
    }
}
