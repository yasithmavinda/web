using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Interfaces.Repositories;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.DTOs.Auth;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using JwtSettings = TaskFlow.Application.Common.Settings.JwtSettings;
using SecuritySettings = TaskFlow.Application.Common.Settings.SecuritySettings;

namespace TaskFlow.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _tokenRepo;
    private readonly IPasswordResetTokenRepository _resetRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;
    private readonly JwtSettings _jwt;
    private readonly SecuritySettings _sec;
    private readonly ILogger<AuthService> _log;

    public AuthService(
        IUserRepository userRepo, IRefreshTokenRepository tokenRepo,
        IPasswordResetTokenRepository resetRepo, IAuditLogRepository auditRepo,
        IJwtService jwtService, IPasswordHasher hasher, IEmailService emailService,
        IMapper mapper, IOptions<JwtSettings> jwt, IOptions<SecuritySettings> sec,
        ILogger<AuthService> log)
    {
        _userRepo = userRepo; _tokenRepo = tokenRepo; _resetRepo = resetRepo;
        _auditRepo = auditRepo; _jwtService = jwtService; _hasher = hasher;
        _emailService = emailService; _mapper = mapper;
        _jwt = jwt.Value; _sec = sec.Value; _log = log;
    }

    // ── REGISTER ─────────────────────────────────────────────
    public async Task<AuthResultDto> RegisterAsync(RegisterDto dto, string ip, CancellationToken ct = default)
    {
        if (await _userRepo.EmailExistsAsync(dto.Email.ToLower(), ct))
            throw new ConflictException($"Email '{dto.Email}' is already registered.");

        var (hash, salt) = _hasher.HashPassword(dto.Password);
        var (rawVerify, verifyHash) = _jwtService.GenerateOneTimeToken();

        var user = new User
        {
            FullName          = dto.FullName.Trim(),
            Email             = dto.Email.ToLower().Trim(),
            PasswordHash      = hash,
            PasswordSalt      = salt,
            EmailVerifyToken  = Convert.ToBase64String(verifyHash),
            EmailVerifyExpiry = DateTime.UtcNow.AddHours(_jwt.EmailVerifyTokenExpiryHours),
            IsEmailVerified   = !_sec.RequireEmailVerification,
            UserRoles         = [new UserRole { RoleId = dto.RoleId, AssignedAt = DateTime.UtcNow }],
        };

        await _userRepo.CreateAsync(user, ct);

        if (_sec.RequireEmailVerification)
            _ = Task.Run(() => _emailService.SendEmailVerificationAsync(user.Email, user.FullName, rawVerify), ct);

        await _auditRepo.LogAsync(new AuditLog { UserId = user.Id, Email = user.Email, Action = AuditActions.RegisterSuccess, IpAddress = ip }, ct);
        _log.LogInformation("User registered: {Id} {Email}", user.Id, user.Email);
        return await BuildAuthResultAsync(user, ip, null, ct);
    }

    // ── LOGIN ────────────────────────────────────────────────
    public async Task<AuthResultDto> LoginAsync(LoginDto dto, string ip, string? ua, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email.ToLower(), ct);
        if (user is null)
        {
            await LogFail(null, dto.Email, ip, "User not found", ct);
            throw new UnauthorizedException("Invalid email or password.");
        }

        if (user.IsLocked)
        {
            await LogFail(user.Id, user.Email, ip, "Account locked", ct);
            throw new AccountLockedException(user.LockoutUntil);
        }

        if (!user.IsActive || user.IsDeleted)
        {
            await LogFail(user.Id, user.Email, ip, "Account inactive", ct);
            throw new UnauthorizedException("Your account has been deactivated. Contact support.");
        }

        if (_sec.RequireEmailVerification && !user.IsEmailVerified)
            throw new UnauthorizedException("Please verify your email before logging in.");

        if (!_hasher.VerifyPassword(dto.Password, user.PasswordHash, user.PasswordSalt))
        {
            user.IncrementFailedLogins();
            await _userRepo.UpdateAsync(user, ct);
            await LogFail(user.Id, user.Email, ip, $"Wrong password attempt #{user.FailedLoginAttempts}", ct);
            _log.LogWarning("Failed login: {Email} from {IP} attempt #{N}", user.Email, ip, user.FailedLoginAttempts);
            if (user.IsLocked) throw new AccountLockedException(user.LockoutUntil);
            throw new UnauthorizedException("Invalid email or password.");
        }

        var activeCount = await _tokenRepo.CountActiveTokensAsync(user.Id, ct);
        if (activeCount >= _sec.MaxActiveRefreshTokens)
            await _tokenRepo.RevokeOldestForUserAsync(user.Id, "Max sessions exceeded", ct);

        user.RecordLogin(ip);
        await _userRepo.UpdateAsync(user, ct);
        await _auditRepo.LogAsync(new AuditLog { UserId = user.Id, Email = user.Email, Action = AuditActions.LoginSuccess, IpAddress = ip, UserAgent = ua }, ct);
        _log.LogInformation("Login: {Id} from {IP}", user.Id, ip);
        return await BuildAuthResultAsync(user, ip, ua, ct);
    }

    // ── REFRESH TOKEN ────────────────────────────────────────
    public async Task<AuthResultDto> RefreshTokenAsync(string rawToken, CancellationToken ct = default)
    {
        var hash   = _jwtService.HashToken(rawToken);
        var stored = await _tokenRepo.GetByHashAsync(hash, ct);
        if (stored is null) throw new UnauthorizedException("Invalid refresh token.");

        if (stored.IsRevoked)
        {
            _log.LogCritical("Revoked token reuse for user {Id} — revoking all sessions", stored.UserId);
            await _tokenRepo.RevokeAllForUserAsync(stored.UserId, "Revoked token reuse", ct);
            throw new UnauthorizedException("Security alert: Please login again.");
        }

        if (stored.IsExpired) throw new UnauthorizedException("Refresh token expired. Please login again.");

        var user = await _userRepo.GetByIdAsync(stored.UserId, ct) ?? throw new UnauthorizedException("User not found.");
        if (!user.CanLogin) throw new UnauthorizedException("Account is inactive or locked.");

        stored.Revoke("token_rotated");
        await _tokenRepo.UpdateAsync(stored, ct);
        await _auditRepo.LogAsync(new AuditLog { UserId = user.Id, Action = AuditActions.TokenRefreshed }, ct);

        return await BuildAuthResultAsync(user, stored.IpAddress ?? "unknown", stored.DeviceInfo, ct);
    }

    // ── LOGOUT ───────────────────────────────────────────────
    public async Task LogoutAsync(long userId, string rawToken, CancellationToken ct = default)
    {
        var hash   = _jwtService.HashToken(rawToken);
        var stored = await _tokenRepo.GetByHashAsync(hash, ct);
        if (stored != null && stored.UserId == userId)
        {
            stored.Revoke("user_logout");
            await _tokenRepo.UpdateAsync(stored, ct);
        }
        await _auditRepo.LogAsync(new AuditLog { UserId = userId, Action = AuditActions.Logout }, ct);
    }

    public async Task LogoutAllDevicesAsync(long userId, CancellationToken ct = default)
        => await _tokenRepo.RevokeAllForUserAsync(userId, "logout_all_devices", ct);

    // ── CHANGE PASSWORD ──────────────────────────────────────
    public async Task ChangePasswordAsync(long userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct) ?? throw new NotFoundException("User", userId);
        if (!_hasher.VerifyPassword(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            throw new UnauthorizedException("Current password is incorrect.");
        if (_hasher.VerifyPassword(dto.NewPassword, user.PasswordHash, user.PasswordSalt))
            throw new BadRequestException("New password must differ from current password.");

        var (h, s) = _hasher.HashPassword(dto.NewPassword);
        user.PasswordHash = h; user.PasswordSalt = s; user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, ct);
        await _tokenRepo.RevokeAllForUserAsync(userId, "password_changed", ct);
        await _auditRepo.LogAsync(new AuditLog { UserId = userId, Action = AuditActions.PasswordChanged }, ct);
        _log.LogInformation("Password changed: {Id}", userId);
    }

    // ── FORGOT PASSWORD ──────────────────────────────────────
    public async Task ForgotPasswordAsync(string email, string ip, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByEmailAsync(email.ToLower(), ct);
        await _auditRepo.LogAsync(new AuditLog { UserId = user?.Id, Email = email, Action = AuditActions.PasswordResetReq, IpAddress = ip }, ct);
        if (user is null || !user.IsActive || user.IsDeleted) return; // Silently return

        await _resetRepo.InvalidateAllForUserAsync(user.Id, ct);
        var (rawToken, tokenHash) = _jwtService.GenerateOneTimeToken();
        await _resetRepo.CreateAsync(new PasswordResetToken
        {
            UserId = user.Id, TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(_jwt.ResetTokenExpiryHours), IpAddress = ip,
        }, ct);
        _ = Task.Run(() => _emailService.SendPasswordResetAsync(user.Email, user.FullName, rawToken), ct);
    }

    // ── RESET PASSWORD ───────────────────────────────────────
    public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
    {
        var hash  = _jwtService.HashToken(dto.Token);
        var token = await _resetRepo.GetByHashAsync(hash, ct);
        if (token is null || !token.IsValid)
            throw new BadRequestException("This password reset link is invalid or has expired.");

        var user = await _userRepo.GetByIdAsync(token.UserId, ct) ?? throw new NotFoundException("User", token.UserId);
        if (_hasher.VerifyPassword(dto.NewPassword, user.PasswordHash, user.PasswordSalt))
            throw new BadRequestException("New password must differ from previous password.");

        var (h, s) = _hasher.HashPassword(dto.NewPassword);
        user.PasswordHash = h; user.PasswordSalt = s; user.UpdatedAt = DateTime.UtcNow;
        token.MarkUsed();
        await _userRepo.UpdateAsync(user, ct);
        await _resetRepo.UpdateAsync(token, ct);
        await _tokenRepo.RevokeAllForUserAsync(user.Id, "password_reset", ct);
        await _auditRepo.LogAsync(new AuditLog { UserId = user.Id, Action = AuditActions.PasswordResetDone }, ct);
        _log.LogInformation("Password reset completed: {Id}", user.Id);
    }

    // ── EMAIL VERIFY ─────────────────────────────────────────
    public async Task<bool> VerifyEmailAsync(string rawToken, CancellationToken ct = default)
    {
        var b64  = Convert.ToBase64String(_jwtService.HashToken(rawToken));
        var user = await _userRepo.GetByEmailVerifyTokenAsync(b64, ct);
        if (user is null || user.IsEmailVerified || user.EmailVerifyExpiry < DateTime.UtcNow) return false;
        user.IsEmailVerified = true; user.EmailVerifyToken = null; user.EmailVerifyExpiry = null; user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user, ct);
        await _auditRepo.LogAsync(new AuditLog { UserId = user.Id, Action = AuditActions.EmailVerified }, ct);
        return true;
    }

    // ── ACTIVE SESSIONS ──────────────────────────────────────
    public async Task<IEnumerable<SessionDto>> GetActiveSessionsAsync(long userId, CancellationToken ct = default)
    {
        var tokens = await _tokenRepo.GetActiveForUserAsync(userId, ct);
        return tokens.Select(t => new SessionDto
        {
            SessionId  = t.TokenId, DeviceInfo = t.DeviceInfo ?? "Unknown",
            IpAddress  = t.IpAddress ?? "Unknown", CreatedAt = t.CreatedAt,
            LastUsedAt = t.LastUsedAt, ExpiresAt  = t.ExpiresAt,
        });
    }

    // ── HELPERS ──────────────────────────────────────────────
    private async Task<AuthResultDto> BuildAuthResultAsync(User user, string ip, string? ua, CancellationToken ct)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var (rawRefresh, refreshHash) = _jwtService.GenerateRefreshToken();
        await _tokenRepo.CreateAsync(new RefreshToken
        {
            UserId = user.Id, TokenHash = refreshHash, DeviceInfo = ua,
            IpAddress = ip, ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays),
        }, ct);
        return new AuthResultDto
        {
            AccessToken = accessToken, RefreshToken = rawRefresh,
            ExpiresIn = _jwt.AccessTokenExpiryMinutes * 60,
            User = _mapper.Map<UserSummaryDto>(user),
        };
    }

    private async Task LogFail(long? uid, string email, string ip, string reason, CancellationToken ct)
        => await _auditRepo.LogAsync(new AuditLog
        {
            UserId = uid, Email = email, Action = AuditActions.LoginFailed,
            IpAddress = ip, IsSuccess = false, FailureReason = reason,
        }, ct);
}

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterDto dto, string ip, CancellationToken ct = default);
    Task<AuthResultDto> LoginAsync(LoginDto dto, string ip, string? ua, CancellationToken ct = default);
    Task<AuthResultDto> RefreshTokenAsync(string rawToken, CancellationToken ct = default);
    Task LogoutAsync(long userId, string rawToken, CancellationToken ct = default);
    Task LogoutAllDevicesAsync(long userId, CancellationToken ct = default);
    Task ChangePasswordAsync(long userId, ChangePasswordDto dto, CancellationToken ct = default);
    Task ForgotPasswordAsync(string email, string ip, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default);
    Task<bool> VerifyEmailAsync(string rawToken, CancellationToken ct = default);
    Task<IEnumerable<SessionDto>> GetActiveSessionsAsync(long userId, CancellationToken ct = default);
}
