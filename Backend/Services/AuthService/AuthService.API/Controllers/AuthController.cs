using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Register a new Customer or Partner account</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        _logger.LogInformation("Registration attempt for email: {Email}, role: {Role}", dto.Email, dto.Role);
        
        try
        {
            var result = await _authService.RegisterAsync(dto);
            _logger.LogInformation("User registered successfully, email: {Email}, role: {Role}", dto.Email, result.Role);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Account created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration failed - conflict: {Message}, email: {Email}", ex.Message, dto.Email);
            return Conflict(ApiResponse<AuthResponseDto>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Registration failed - validation: {Message}, email: {Email}", ex.Message, dto.Email);
            return BadRequest(ApiResponse<AuthResponseDto>.Fail(ex.Message));
        }
    }

    /// <summary>Login and get JWT token (OTP sent if 2FA is enabled)</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        _logger.LogInformation("[AUTH] Login attempt for email: {Email}", dto.Email);
        
        try
        {
            var result = await _authService.LoginAsync(dto);
            _logger.LogInformation("[AUTH] Login successful for email: {Email}, Role: {Role}, RequiresOtp: {RequiresOtp}", 
                dto.Email, result.Role, result.RequiresOtp);
            
            // Log token info (first 20 chars only for security)
            _logger.LogDebug("[AUTH] Access token generated (preview): {TokenPreview}...", 
                result.AccessToken.Substring(0, Math.Min(20, result.AccessToken.Length)));
            
            var response = ApiResponse<AuthResponseDto>.Ok(result,
                result.RequiresOtp ? "OTP sent to your email." : "Login successful.");
            
            _logger.LogDebug("[AUTH] Response structure: Success={Success}, HasData={HasData}", 
                response.Success, response.Data != null);
            
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("[AUTH] Login failed for email: {Email}, reason: {Message}", dto.Email, ex.Message);
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUTH] Unexpected error during login for email: {Email}", dto.Email);
            return StatusCode(500, ApiResponse<AuthResponseDto>.Fail("An unexpected error occurred during login."));
        }
    }

    /// <summary>Verify 2FA OTP and complete login</summary>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        _logger.LogInformation("OTP verification attempt for email: {Email}", dto.Email);
        
        try
        {
            var result = await _authService.VerifyOtpAsync(dto);
            _logger.LogInformation("OTP verified successfully for email: {Email}", dto.Email);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "OTP verified. Login successful."));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("OTP verification failed for email: {Email}, reason: {Message}", dto.Email, ex.Message);
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail(ex.Message));
        }
    }

    /// <summary>Refresh access token using refresh token</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        _logger.LogInformation("[AUTH] Token refresh attempt");
        
        try
        {
            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
            _logger.LogInformation("[AUTH] Token refreshed successfully, Role: {Role}", result.Role);
            _logger.LogDebug("[AUTH] New access token generated (preview): {TokenPreview}...", 
                result.AccessToken.Substring(0, Math.Min(20, result.AccessToken.Length)));
            
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Token refreshed successfully."));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("[AUTH] Token refresh failed - Unauthorized: {Message}", ex.Message);
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUTH] Unexpected error during token refresh");
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail("Token refresh failed."));
        }
    }

    /// <summary>Send OTP to user's email for verification or password reset</summary>
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpDto dto)
    {
        _logger.LogInformation("Send OTP request for email: {Email}", dto.Email);
        
        try
        {
            var message = await _authService.SendOtpAsync(dto);
            _logger.LogInformation("OTP sent successfully to email: {Email}", dto.Email);
            return Ok(ApiResponse<string>.Ok(message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Send OTP failed for email: {Email}, reason: {Message}", dto.Email, ex.Message);
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Verify email using OTP</summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        _logger.LogInformation("Email verification attempt for email: {Email}", dto.Email);
        
        try
        {
            var message = await _authService.VerifyEmailAsync(dto);
            _logger.LogInformation("Email verified successfully: {Email}", dto.Email);
            return Ok(ApiResponse<string>.Ok(message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Email verification failed for email: {Email}, reason: {Message}", dto.Email, ex.Message);
            return Unauthorized(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Enable or disable two-factor authentication (requires authentication)</summary>
    [HttpPost("toggle-2fa")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Toggle2FA([FromBody] Toggle2FADto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) 
                           ?? User.FindFirst("sub") 
                           ?? throw new UnauthorizedAccessException("User ID not found in token");
            
            var userId = Guid.Parse(userIdClaim.Value);
            _logger.LogInformation("Toggle 2FA request for user: {UserId}, enable: {Enable}", userId, dto.Enable);
            
            var message = await _authService.Toggle2FAAsync(userId, dto.Enable);
            _logger.LogInformation("2FA toggled successfully for user: {UserId}", userId);
            return Ok(ApiResponse<string>.Ok(message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Toggle 2FA failed");
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Resend OTP to user's email</summary>
    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] SendOtpDto dto)
    {
        _logger.LogInformation("Resend OTP request for email: {Email}", dto.Email);
        
        try
        {
            var message = await _authService.ResendOtpAsync(dto.Email);
            _logger.LogInformation("OTP resent successfully to email: {Email}", dto.Email);
            return Ok(ApiResponse<string>.Ok(message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Resend OTP failed for email: {Email}, reason: {Message}", dto.Email, ex.Message);
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Request password reset OTP</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        _logger.LogInformation("Forgot password request for email: {Email}", dto.Email);
        
        try
        {
            var message = await _authService.ForgotPasswordAsync(dto);
            _logger.LogInformation("Password reset OTP sent to email: {Email}", dto.Email);
            return Ok(ApiResponse<string>.Ok(message));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Forgot password failed for email: {Email}, reason: {Message}", dto.Email, ex.Message);
            // Return success message even if user not found (security best practice)
            return Ok(ApiResponse<string>.Ok("If an account exists with this email, a password reset code has been sent."));
        }
    }

    /// <summary>Reset password using OTP</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        _logger.LogInformation("Reset password attempt for email: {Email}", dto.Email);
        
        try
        {
            var message = await _authService.ResetPasswordAsync(dto);
            _logger.LogInformation("Password reset successfully for email: {Email}", dto.Email);
            return Ok(ApiResponse<string>.Ok(message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Reset password failed for email: {Email}, reason: {Message}", dto.Email, ex.Message);
            return Unauthorized(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Change password for authenticated user</summary>
    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) 
                           ?? User.FindFirst("sub") 
                           ?? throw new UnauthorizedAccessException("User ID not found in token");
            
            var userId = Guid.Parse(userIdClaim.Value);
            _logger.LogInformation("Change password request for user: {UserId}", userId);
            
            var message = await _authService.ChangePasswordAsync(userId, dto);
            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
            return Ok(ApiResponse<string>.Ok(message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Change password failed: {Message}", ex.Message);
            return Unauthorized(ApiResponse<string>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change password failed");
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Get current user profile</summary>
    [HttpGet("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) 
                           ?? User.FindFirst("sub") 
                           ?? throw new UnauthorizedAccessException("User ID not found in token");
            
            var userId = Guid.Parse(userIdClaim.Value);
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                         ?? User.FindFirst("email")?.Value 
                         ?? "unknown";
            
            _logger.LogInformation("[AUTH] Get profile request for user: {UserId}, Email: {Email}", userId, userEmail);
            
            var profile = await _authService.GetProfileAsync(userId);
            
            _logger.LogInformation("[AUTH] Profile retrieved successfully for user: {UserId}, Role: {Role}", 
                userId, profile.Role);
            
            return Ok(ApiResponse<UserProfileDto>.Ok(profile));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("[AUTH] Get profile failed - Unauthorized: {Message}", ex.Message);
            return Unauthorized(ApiResponse<UserProfileDto>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AUTH] Get profile failed with unexpected error");
            return BadRequest(ApiResponse<UserProfileDto>.Fail("Failed to retrieve profile."));
        }
    }

    /// <summary>Update current user profile</summary>
    [HttpPut("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier) 
                           ?? User.FindFirst("sub") 
                           ?? throw new UnauthorizedAccessException("User ID not found in token");
            
            var userId = Guid.Parse(userIdClaim.Value);
            _logger.LogInformation("Update profile request for user: {UserId}", userId);
            
            var profile = await _authService.UpdateProfileAsync(userId, dto);
            _logger.LogInformation("Profile updated successfully for user: {UserId}", userId);
            return Ok(ApiResponse<UserProfileDto>.Ok(profile, "Profile updated successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update profile failed");
            return BadRequest(ApiResponse<UserProfileDto>.Fail(ex.Message));
        }
    }

    /// <summary>Admin endpoint to approve/reject delivery agent</summary>
    [HttpPost("admin/approve-agent")]
    public async Task<IActionResult> ApproveAgent([FromBody] ApproveAgentRequestDto dto)
    {
        try
        {
            await _authService.UpdateAgentApprovalAsync(dto.UserId, dto.IsApproved, dto.ApprovedBy, dto.Notes);
            _logger.LogInformation("Agent {UserId} approval status updated to {IsApproved}", dto.UserId, dto.IsApproved);
            return Ok(new { message = "Agent approval status updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update agent approval status");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Admin endpoint to toggle user active status</summary>
    [HttpPost("admin/toggle-user-status")]
    public async Task<IActionResult> ToggleUserStatus([FromBody] ToggleUserStatusRequestDto dto)
    {
        try
        {
            await _authService.UpdateUserActiveStatusAsync(dto.UserId, dto.IsActive, dto.Reason);
            _logger.LogInformation("User {UserId} active status updated to {IsActive}", dto.UserId, dto.IsActive);
            return Ok(new { message = "User status updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user status");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Admin endpoint to soft delete a user</summary>
    [HttpDelete("admin/users/{userId}")]
    public async Task<IActionResult> SoftDeleteUser(Guid userId, [FromBody] SoftDeleteUserRequestDto dto)
    {
        try
        {
            await _authService.SoftDeleteUserAsync(userId, dto.DeletedBy, dto.Reason);
            _logger.LogInformation("User {UserId} soft deleted by {DeletedBy}", userId, dto.DeletedBy);
            return Ok(new { message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to soft delete user");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Admin endpoint to restore a soft-deleted user</summary>
    [HttpPost("admin/users/{userId}/restore")]
    public async Task<IActionResult> RestoreUser(Guid userId, [FromBody] RestoreUserRequestDto dto)
    {
        try
        {
            await _authService.RestoreUserAsync(userId, dto.RestoredBy, dto.Reason);
            _logger.LogInformation("User {UserId} restored by {RestoredBy}", userId, dto.RestoredBy);
            return Ok(new { message = "User restored successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore user");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Admin endpoint to get user details</summary>
    [HttpGet("admin/users/{userId}")]
    public async Task<IActionResult> GetUserDetails(Guid userId)
    {
        try
        {
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new { error = "User not found" });
            
            return Ok(ApiResponse<UserProfileDto>.Ok(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user details");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Admin endpoint to get all users by role</summary>
    [HttpGet("admin/users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] string? role, [FromQuery] bool? isActive)
    {
        try
        {
            var users = await _authService.GetAllUsersAsync(role, isActive);
            _logger.LogInformation("Retrieved {Count} users with role filter: {Role}", users.Count, role ?? "all");
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Admin endpoint to toggle user email verification</summary>
    [HttpPatch("admin/users/{userId}/toggle-verification")]
    public async Task<IActionResult> ToggleEmailVerification(Guid userId, [FromBody] ToggleVerificationRequestDto dto)
    {
        try
        {
            await _authService.ToggleEmailVerificationAsync(userId, dto.IsVerified, dto.AdminId, dto.Reason);
            _logger.LogInformation("User {UserId} email verification toggled to {IsVerified} by admin {AdminId}", userId, dto.IsVerified, dto.AdminId);
            return Ok(new { message = $"User email verification {(dto.IsVerified ? "enabled" : "disabled")} successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle email verification");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Internal: Get basic user info by ID (for inter-service calls)</summary>
    [HttpGet("users/{id:guid}/info")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> GetUserInfo(Guid id)
    {
        try
        {
            var profile = await _authService.GetProfileAsync(id);
            return Ok(new { success = true, data = new { fullName = profile.FullName, email = profile.Email } });
        }
        catch
        {
            return NotFound(new { success = false });
        }
    }
}

public class ApproveAgentRequestDto
{
    public Guid UserId { get; set; }
    public bool IsApproved { get; set; }
    public Guid ApprovedBy { get; set; }
    public string? Notes { get; set; }
}

public class ToggleUserStatusRequestDto
{
    public Guid UserId { get; set; }
    public bool IsActive { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class SoftDeleteUserRequestDto
{
    [Required]
    public Guid DeletedBy { get; set; }
    
    [Required, MinLength(10)]
    public string Reason { get; set; } = string.Empty;
}

public class RestoreUserRequestDto
{
    [Required]
    public Guid RestoredBy { get; set; }
    
    [Required, MinLength(10)]
    public string Reason { get; set; } = string.Empty;
}

public class ToggleVerificationRequestDto
{
    [Required]
    public bool IsVerified { get; set; }
    
    [Required]
    public Guid AdminId { get; set; }
    
    [Required, MinLength(5)]
    public string Reason { get; set; } = string.Empty;
}
