using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using FoodDelivery.Shared.Common;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Register a new Customer or Partner account</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var result = await _authService.RegisterAsync(dto);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Account created successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<AuthResponseDto>.Fail(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<AuthResponseDto>.Fail(ex.Message));
        }
    }

    /// <summary>Login and get JWT token (OTP sent if 2FA is enabled)</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result,
                result.RequiresOtp ? "OTP sent to your email." : "Login successful."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail(ex.Message));
        }
    }

    /// <summary>Verify 2FA OTP and complete login</summary>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        try
        {
            var result = await _authService.VerifyOtpAsync(dto);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "OTP verified. Login successful."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail(ex.Message));
        }
    }

    /// <summary>Refresh access token using refresh token</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result));
        }
        catch (Exception ex)
        {
            return Unauthorized(ApiResponse<AuthResponseDto>.Fail(ex.Message));
        }
    }

    /// <summary>Send OTP to user's email for verification or password reset</summary>
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpDto dto)
    {
        try
        {
            var message = await _authService.SendOtpAsync(dto);
            return Ok(ApiResponse<string>.Ok(message));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Verify email using OTP</summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        try
        {
            var message = await _authService.VerifyEmailAsync(dto);
            return Ok(ApiResponse<string>.Ok(message));
        }
        catch (UnauthorizedAccessException ex)
        {
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
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
            var message = await _authService.Toggle2FAAsync(userId, dto.Enable);
            return Ok(ApiResponse<string>.Ok(message));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<string>.Fail(ex.Message));
        }
    }

    /// <summary>Resend OTP to user's email</summary>
    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] SendOtpDto dto)
    {
        try
        {
            var message = await _authService.ResendOtpAsync(dto.Email);
            return Ok(ApiResponse<string>.Ok(message));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<string>.Fail(ex.Message));
        }
    }
}