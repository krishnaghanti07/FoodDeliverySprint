using System.Security.Claims;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth/wallet")]
[Authorize(Roles = "Customer")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    /// <summary>Get current wallet balance</summary>
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        try
        {
            var userId = GetUserId();
            var balance = await _walletService.GetBalanceAsync(userId);
            return Ok(ApiResponse<decimal>.Ok(balance, "Wallet balance retrieved successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<decimal>.Fail(ex.Message));
        }
    }

    /// <summary>Get wallet transaction history</summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions()
    {
        try
        {
            var userId = GetUserId();
            var transactions = await _walletService.GetTransactionsAsync(userId);
            
            // Map to a cleaner DTO without the full User object
            var transactionDtos = transactions.Select(t => new
            {
                t.Id,
                t.UserId,
                t.Amount,
                Type = t.Type.ToString(),
                Source = t.Source.ToString(),
                t.ReferenceId,
                t.Description,
                t.CreatedAt
            }).ToList();
            
            return Ok(ApiResponse<object>.Ok(transactionDtos, "Wallet transactions retrieved successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>Deduct amount from wallet (for order payments)</summary>
    [HttpPost("deduct")]
    [AllowAnonymous] // Allow internal service calls
    public async Task<IActionResult> DeductFromWallet([FromBody] WalletDeductRequest request)
    {
        try
        {
            var success = await _walletService.DebitWalletAsync(
                request.UserId, 
                request.Amount, 
                request.Source, 
                request.ReferenceId, 
                request.Description
            );

            if (!success)
            {
                return BadRequest(ApiResponse<bool>.Fail("Insufficient wallet balance or invalid request."));
            }

            var newBalance = await _walletService.GetBalanceAsync(request.UserId);
            return Ok(ApiResponse<object>.Ok(new { success = true, newBalance }, "Amount deducted successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>Add amount to wallet (for refunds)</summary>
    [HttpPost("add")]
    [AllowAnonymous] // Allow internal service calls
    public async Task<IActionResult> AddToWallet([FromBody] WalletAddRequest request)
    {
        try
        {
            var success = await _walletService.AddCreditAsync(
                request.UserId, 
                request.Amount, 
                request.Source, 
                request.ReferenceId, 
                request.Description
            );

            if (!success)
            {
                return BadRequest(ApiResponse<bool>.Fail("Invalid request or user not found."));
            }

            var newBalance = await _walletService.GetBalanceAsync(request.UserId);
            return Ok(ApiResponse<object>.Ok(new { success = true, newBalance }, "Amount added successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>Sync wallet balance with transaction history (for existing balances without transactions)</summary>
    [HttpPost("sync")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> SyncWalletBalance()
    {
        try
        {
            var userId = GetUserId();
            var balance = await _walletService.GetBalanceAsync(userId);
            var transactions = await _walletService.GetTransactionsAsync(userId);

            // If user has balance but no transactions, create a sync transaction record only
            if (balance > 0 && transactions.Count == 0)
            {
                var success = await _walletService.CreateSyncTransactionAsync(
                    userId,
                    balance,
                    TransactionSource.AdminCredit,
                    null,
                    "Wallet balance (existing credit)"
                );

                if (success)
                {
                    var updatedTransactions = await _walletService.GetTransactionsAsync(userId);
                    var transactionDtos = updatedTransactions.Select(t => new
                    {
                        t.Id,
                        t.UserId,
                        t.Amount,
                        Type = t.Type.ToString(),
                        Source = t.Source.ToString(),
                        t.ReferenceId,
                        t.Description,
                        t.CreatedAt
                    }).ToList();
                    
                    return Ok(ApiResponse<object>.Ok(new { 
                        synced = true, 
                        transactions = transactionDtos 
                    }, "Wallet history synced successfully."));
                }
            }

            return Ok(ApiResponse<object>.Ok(new { 
                synced = false, 
                transactionsCount = transactions.Count 
            }, "No sync needed."));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

// Request DTOs
public class WalletDeductRequest
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public TransactionSource Source { get; set; }
    public Guid? ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class WalletAddRequest
{
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public TransactionSource Source { get; set; }
    public Guid? ReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
}
