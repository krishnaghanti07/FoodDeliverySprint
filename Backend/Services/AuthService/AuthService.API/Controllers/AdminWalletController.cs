using System.Security.Claims;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/admin/wallet")]
[Authorize(Roles = "Admin")]
public class AdminWalletController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly ILogger<AdminWalletController> _logger;

    public AdminWalletController(IWalletService walletService, ILogger<AdminWalletController> logger)
    {
        _walletService = walletService;
        _logger = logger;
    }

    /// <summary>Admin: Credit wallet for refund or other purposes</summary>
    [HttpPost("credit")]
    public async Task<IActionResult> CreditWallet([FromBody] AdminCreditWalletDto dto)
    {
        try
        {
            var adminId = GetUserId();
            _logger.LogInformation("Admin {AdminId} crediting {Amount} to user {UserId}", 
                adminId, dto.Amount, dto.UserId);

            var source = string.IsNullOrEmpty(dto.Source) ? TransactionSource.AdminCredit : 
                Enum.Parse<TransactionSource>(dto.Source, true);

            var success = await _walletService.AddCreditAsync(
                dto.UserId, 
                dto.Amount, 
                source, 
                dto.ReferenceId, 
                dto.Description ?? $"Admin credit by {adminId}");

            if (!success)
            {
                return BadRequest(ApiResponse<object>.Fail("Failed to credit wallet. Invalid amount or user not found."));
            }

            return Ok(ApiResponse<object>.Ok(new { userId = dto.UserId, amount = dto.Amount }, 
                "Wallet credited successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crediting wallet for user {UserId}", dto.UserId);
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }

    /// <summary>Admin: Get user's wallet balance</summary>
    [HttpGet("{userId}/balance")]
    public async Task<IActionResult> GetUserBalance(Guid userId)
    {
        try
        {
            var balance = await _walletService.GetBalanceAsync(userId);
            return Ok(ApiResponse<decimal>.Ok(balance));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wallet balance for user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }

    /// <summary>Admin: Get user's wallet transactions</summary>
    [HttpGet("{userId}/transactions")]
    public async Task<IActionResult> GetUserTransactions(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var transactions = await _walletService.GetTransactionsAsync(userId, page, pageSize);
            return Ok(ApiResponse<object>.Ok(transactions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wallet transactions for user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

public class AdminCreditWalletDto
{
    [System.Text.Json.Serialization.JsonPropertyName("userId")]
    public Guid UserId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("source")]
    public string? Source { get; set; } // "Refund", "AdminCredit", etc.
    
    [System.Text.Json.Serialization.JsonPropertyName("referenceId")]
    public Guid? ReferenceId { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; set; }
}
