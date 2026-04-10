using System;
using System.Collections.Generic;
using System.Text;
using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> VerifyOtpAsync(VerifyOtpDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    
    // OTP Management
    Task<string> SendOtpAsync(SendOtpDto dto);
    Task<string> VerifyEmailAsync(VerifyEmailDto dto);
    Task<string> Toggle2FAAsync(Guid userId, bool enable);
    Task<string> ResendOtpAsync(string email);
}

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode);
}

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, string role);
    string GenerateRefreshToken();
}
