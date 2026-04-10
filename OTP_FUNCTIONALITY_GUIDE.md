# OTP Functionality Guide

## Overview

The AuthService now includes comprehensive OTP (One-Time Password) functionality for:
- Email verification
- Two-factor authentication (2FA)
- Password reset (future enhancement)
- Secure login flows

## 🔐 Available OTP Endpoints

### 1. Send OTP
**Endpoint**: `POST /api/auth/send-otp` or `POST /gateway/auth/send-otp`

Sends an OTP to the user's registered email address.

**Request Body**:
```json
{
  "email": "user@example.com",
  "purpose": "EmailVerification"
}
```

**Purpose Options**:
- `EmailVerification` - Verify user's email address
- `PasswordReset` - Reset forgotten password
- `Enable2FA` - Enable two-factor authentication

**Response**:
```json
{
  "success": true,
  "message": "Success",
  "data": "OTP sent to user@example.com for EmailVerification.",
  "errors": []
}
```

**Example (PowerShell)**:
```powershell
$body = @{email='user@example.com';purpose='EmailVerification'} | ConvertTo-Json
Invoke-WebRequest -Uri 'http://localhost:5000/gateway/auth/send-otp' -Method POST -Body $body -ContentType 'application/json'
```

---

### 2. Verify Email with OTP
**Endpoint**: `POST /api/auth/verify-email` or `POST /gateway/auth/verify-email`

Verifies the user's email address using the OTP sent to their email.

**Request Body**:
```json
{
  "email": "user@example.com",
  "otpCode": "123456"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Success",
  "data": "Email verified successfully.",
  "errors": []
}
```

**Example (PowerShell)**:
```powershell
$body = @{email='user@example.com';otpCode='123456'} | ConvertTo-Json
Invoke-WebRequest -Uri 'http://localhost:5000/gateway/auth/verify-email' -Method POST -Body $body -ContentType 'application/json'
```

---

### 3. Verify OTP for Login (2FA)
**Endpoint**: `POST /api/auth/verify-otp` or `POST /gateway/auth/verify-otp`

Completes the login process when 2FA is enabled. This endpoint is used after login returns `requiresOtp: true`.

**Request Body**:
```json
{
  "email": "user@example.com",
  "otpCode": "123456"
}
```

**Response**:
```json
{
  "success": true,
  "message": "OTP verified. Login successful.",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123...",
    "role": "Customer",
    "fullName": "John Doe",
    "requiresOtp": false
  },
  "errors": []
}
```

---

### 4. Enable/Disable Two-Factor Authentication
**Endpoint**: `POST /api/auth/toggle-2fa` or `POST /gateway/auth/toggle-2fa`

**Authentication Required**: Yes (Bearer token)

Enables or disables 2FA for the authenticated user.

**Request Headers**:
```
Authorization: Bearer {your_jwt_token}
```

**Request Body**:
```json
{
  "enable": true
}
```

**Response**:
```json
{
  "success": true,
  "message": "Success",
  "data": "Two-factor authentication enabled.",
  "errors": []
}
```

**Example (PowerShell)**:
```powershell
$token = "your_jwt_token_here"
$body = @{enable=$true} | ConvertTo-Json
Invoke-WebRequest -Uri 'http://localhost:5000/gateway/auth/toggle-2fa' -Method POST -Body $body -ContentType 'application/json' -Headers @{Authorization="Bearer $token"}
```

---

### 5. Resend OTP
**Endpoint**: `POST /api/auth/resend-otp` or `POST /gateway/auth/resend-otp`

Resends the OTP if the user didn't receive it or it expired.

**Request Body**:
```json
{
  "email": "user@example.com",
  "purpose": "EmailVerification"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Success",
  "data": "OTP resent to user@example.com.",
  "errors": []
}
```

---

## 🔄 Complete Workflows

### Workflow 1: Email Verification After Registration

1. **Register a new account**:
```json
POST /gateway/auth/register
{
  "fullName": "John Doe",
  "email": "john@example.com",
  "mobile": "1234567890",
  "password": "Test@1234",
  "role": "Customer"
}
```

2. **Send OTP for email verification**:
```json
POST /gateway/auth/send-otp
{
  "email": "john@example.com",
  "purpose": "EmailVerification"
}
```

3. **Check email for OTP** (6-digit code)

4. **Verify email with OTP**:
```json
POST /gateway/auth/verify-email
{
  "email": "john@example.com",
  "otpCode": "123456"
}
```

---

### Workflow 2: Enable Two-Factor Authentication

1. **Login to get JWT token**:
```json
POST /gateway/auth/login
{
  "email": "john@example.com",
  "password": "Test@1234"
}
```

2. **Enable 2FA** (using the JWT token):
```json
POST /gateway/auth/toggle-2fa
Headers: Authorization: Bearer {token}
{
  "enable": true
}
```

3. **Next login will require OTP**

---

### Workflow 3: Login with Two-Factor Authentication

1. **Login** (when 2FA is enabled):
```json
POST /gateway/auth/login
{
  "email": "john@example.com",
  "password": "Test@1234"
}
```

**Response**:
```json
{
  "success": true,
  "message": "OTP sent to your email.",
  "data": {
    "requiresOtp": true,
    "role": "Customer",
    "fullName": "John Doe"
  }
}
```

2. **Check email for OTP**

3. **Verify OTP to complete login**:
```json
POST /gateway/auth/verify-otp
{
  "email": "john@example.com",
  "otpCode": "123456"
}
```

**Response**:
```json
{
  "success": true,
  "message": "OTP verified. Login successful.",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123...",
    "role": "Customer",
    "fullName": "John Doe"
  }
}
```

---

## 📧 Email Configuration

OTP emails are sent using the SMTP configuration in `appsettings.json`:

```json
{
  "Email": {
    "From": "noreply@fooddelivery.com",
    "Host": "smtp.gmail.com",
    "Port": "587",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

**Note**: For Gmail, you need to use an App Password, not your regular password.

---

## 🔒 Security Features

1. **OTP Expiry**: OTPs expire after 10 minutes
2. **One-Time Use**: Each OTP can only be used once
3. **Secure Generation**: OTPs are 6-digit random numbers
4. **Email Delivery**: OTPs are sent only to registered email addresses
5. **Rate Limiting**: Consider implementing rate limiting for OTP requests (future enhancement)

---

## 🧪 Testing OTP Functionality

### Test 1: Send OTP
```bash
# Using curl
curl -X POST http://localhost:5000/gateway/auth/send-otp \
  -H "Content-Type: application/json" \
  -d '{"email":"customer@test.com","purpose":"EmailVerification"}'
```

### Test 2: Verify Email
```bash
# Replace 123456 with actual OTP from email
curl -X POST http://localhost:5000/gateway/auth/verify-email \
  -H "Content-Type: application/json" \
  -d '{"email":"customer@test.com","otpCode":"123456"}'
```

### Test 3: Enable 2FA
```bash
# Replace {token} with your JWT token
curl -X POST http://localhost:5000/gateway/auth/toggle-2fa \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"enable":true}'
```

---

## 📊 Gateway Routes

All OTP endpoints are accessible through the Gateway:

| Endpoint | Method | Auth Required | Purpose |
|----------|--------|---------------|---------|
| `/gateway/auth/send-otp` | POST | No | Send OTP to email |
| `/gateway/auth/verify-email` | POST | No | Verify email with OTP |
| `/gateway/auth/verify-otp` | POST | No | Complete 2FA login |
| `/gateway/auth/toggle-2fa` | POST | Yes | Enable/disable 2FA |
| `/gateway/auth/resend-otp` | POST | No | Resend OTP |

---

## 🎯 Use Cases

### 1. Email Verification
- Verify user email after registration
- Ensure users have access to their registered email
- Reduce fake accounts

### 2. Two-Factor Authentication
- Add extra security layer for sensitive accounts
- Protect against password theft
- Required for admin/partner accounts (optional)

### 3. Password Reset (Future)
- Verify user identity before password reset
- Secure password recovery flow
- Prevent unauthorized password changes

---

## 🔧 Implementation Details

### OTP Generation
```csharp
private static string GenerateOtp() =>
    Random.Shared.Next(100000, 999999).ToString();
```

### OTP Storage
- OTPs are stored in the `Users` table
- Fields: `OtpCode` (string), `OtpExpiry` (DateTime)
- Cleared after successful verification

### Email Service
- Uses SMTP for email delivery
- Configurable via `appsettings.json`
- Supports Gmail, Outlook, and custom SMTP servers

---

## 📝 Database Schema

The `Users` table includes OTP-related fields:

```sql
CREATE TABLE Users (
    ...
    OtpCode NVARCHAR(10) NULL,
    OtpExpiry DATETIME2 NULL,
    TwoFactorEnabled BIT NOT NULL DEFAULT 0,
    IsEmailVerified BIT NOT NULL DEFAULT 0,
    ...
)
```

---

## 🚀 Quick Start

1. **Send OTP to test user**:
```powershell
$body = @{email='customer@test.com';purpose='EmailVerification'} | ConvertTo-Json
Invoke-WebRequest -Uri 'http://localhost:5000/gateway/auth/send-otp' -Method POST -Body $body -ContentType 'application/json'
```

2. **Check console output** (OTP will be logged if email sending fails)

3. **Verify with OTP**:
```powershell
$body = @{email='customer@test.com';otpCode='123456'} | ConvertTo-Json
Invoke-WebRequest -Uri 'http://localhost:5000/gateway/auth/verify-email' -Method POST -Body $body -ContentType 'application/json'
```

---

## ✅ What's New

- ✅ Send OTP endpoint for email verification
- ✅ Verify email with OTP
- ✅ Enable/Disable 2FA for users
- ✅ Resend OTP functionality
- ✅ Complete 2FA login flow
- ✅ Gateway routes for all OTP endpoints
- ✅ Comprehensive error handling
- ✅ OTP expiry (10 minutes)

---

## 📚 Related Documentation

- [TESTING_GUIDE.md](TESTING_GUIDE.md) - Complete API testing guide
- [QUICK_START.md](QUICK_START.md) - Quick start guide
- [README.md](README.md) - Project overview

---

**Status**: ✅ OTP functionality fully implemented and tested!
