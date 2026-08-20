using System;
using System.Collections.Generic;
using System.Text;
using AuthService.Application.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace AuthService.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    public EmailService(IConfiguration config) => _config = config;

    public async Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("FoodDelivery", _config["Email:From"]));
        message.To.Add(new MailboxAddress(fullName, toEmail));
        message.Subject = "Your FoodDelivery Login OTP";

        message.Body = new TextPart("html")
        {
            Text = $@"
                <h2>Hello {fullName},</h2>
                <p>Your one-time password (OTP) for login is:</p>
                <h1 style='letter-spacing:8px;color:#E85D24'>{otpCode}</h1>
                <p>This OTP is valid for <b>10 minutes</b>. Do not share it with anyone.</p>
                <p>If you didn't request this, please ignore this email.</p>
                <br/><p>— FoodDelivery Team</p>"
        };

        await SendEmailAsync(message);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string fullName, string otpCode)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("FoodDelivery", _config["Email:From"]));
        message.To.Add(new MailboxAddress(fullName, toEmail));
        message.Subject = "Password Reset Request - FoodDelivery";

        message.Body = new TextPart("html")
        {
            Text = $@"
                <h2>Hello {fullName},</h2>
                <p>We received a request to reset your password. Use the code below to reset your password:</p>
                <h1 style='letter-spacing:8px;color:#E85D24'>{otpCode}</h1>
                <p>This code is valid for <b>15 minutes</b>. Do not share it with anyone.</p>
                <p><strong>If you didn't request a password reset, please ignore this email and your password will remain unchanged.</strong></p>
                <br/><p>— FoodDelivery Team</p>"
        };

        await SendEmailAsync(message);
    }

    private async Task SendEmailAsync(MimeMessage message)
    {
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_config["Email:Host"], int.Parse(_config["Email:Port"]!),
            MailKit.Security.SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_config["Email:Username"], _config["Email:Password"]);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}
