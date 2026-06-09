using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskFlow.Application.DTOs.Auth;
using TaskFlow.Application.Services;

namespace TaskFlow.API.Controllers;

/// <summary>Authentication — Login, Register, Token Refresh, Password Management</summary>
[Tags("Authentication")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _log;

    public AuthController(IAuthService auth, ILogger<AuthController> log)
    { _auth = auth; _log = log; }

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(201)]
    [ProducesResponseType(409)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(dto, GetClientIp(), ct);
        return CreatedResponse(result, "Registration successful.");
    }

    /// <summary>Login with email and password. Returns JWT access + refresh token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(423)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        var ua     = Request.Headers.UserAgent.ToString();
        var result = await _auth.LoginAsync(dto, GetClientIp(), ua, ct);
        return OkResponse(result, "Login successful.");
    }

    /// <summary>Exchange a refresh token for a new access + refresh token pair (token rotation).</summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto, CancellationToken ct)
    {
        var result = await _auth.RefreshTokenAsync(dto.RefreshToken, ct);
        return OkResponse(result, "Token refreshed.");
    }

    /// <summary>Logout from the current device. Revokes the refresh token.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto, CancellationToken ct)
    {
        await _auth.LogoutAsync(CurrentUserId!.Value, dto.RefreshToken, ct);
        return OkNoData("Logged out successfully.");
    }

    /// <summary>Logout from ALL devices. Revokes all refresh tokens.</summary>
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        await _auth.LogoutAllDevicesAsync(CurrentUserId!.Value, ct);
        return OkNoData("Logged out from all devices.");
    }

    /// <summary>Request a password reset email. Always returns 200 to prevent email enumeration.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("passwordReset")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken ct)
    {
        await _auth.ForgotPasswordAsync(dto.Email, GetClientIp(), ct);
        return OkNoData("If this email is registered, a reset link has been sent.");
    }

    /// <summary>Reset password using token from email link. Token is single-use and expires in 1 hour.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        await _auth.ResetPasswordAsync(dto, ct);
        return OkNoData("Password reset successful. Please login with your new password.");
    }

    /// <summary>Change password (requires current password). Revokes all sessions.</summary>
    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
    {
        await _auth.ChangePasswordAsync(CurrentUserId!.Value, dto, ct);
        return OkNoData("Password changed. Please login again on all devices.");
    }

    /// <summary>Verify email address using token from verification email.</summary>
    [HttpGet("verify-email/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(string token, CancellationToken ct)
    {
        var verified = await _auth.VerifyEmailAsync(token, ct);
        return verified
            ? OkNoData("Email verified. You can now login.")
            : base.BadRequest(new { success = false, message = "Invalid or expired verification link." });
    }

    /// <summary>Get the currently authenticated user's profile.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me([FromServices] IUserService userSvc, CancellationToken ct)
    {
        var user = await userSvc.GetByIdAsync(CurrentUserId!.Value, ct);
        return OkResponse(user);
    }

    /// <summary>Get all active sessions (devices) for the current user.</summary>
    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        var sessions = await _auth.GetActiveSessionsAsync(CurrentUserId!.Value, ct);
        return OkResponse(sessions);
    }
}
