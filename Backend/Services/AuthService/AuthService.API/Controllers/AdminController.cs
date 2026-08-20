using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAuthService authService, ILogger<AdminController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Get user by ID (Admin only)</summary>
    [HttpGet("users/{userId}")]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(userId);
            
            if (user == null)
                return NotFound(ApiResponse<object>.Fail("User not found."));

            return Ok(ApiResponse<object>.Ok(new
            {
                id = user.Id,
                email = user.Email,
                fullName = user.FullName,
                role = user.Role,
                isEmailVerified = user.IsEmailVerified,
                walletBalance = user.WalletBalance
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user {UserId}", userId);
            return StatusCode(500, ApiResponse<object>.Fail("Internal server error"));
        }
    }
}
