namespace TaskFlow.Domain.Entities;

public class RefreshToken
{
    public long TokenId { get; set; }
    public long UserId { get; set; }
    public byte[] TokenHash { get; set; } = [];
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive  => !IsRevoked && !IsExpired;

    public User User { get; set; } = null!;

    public void Revoke(string reason)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
    }
}

public class PasswordResetToken
{
    public long TokenId { get; set; }
    public long UserId { get; set; }
    public byte[] TokenHash { get; set; } = [];
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid   => !IsUsed && !IsExpired;

    public User User { get; set; } = null!;

    public void MarkUsed() { IsUsed = true; UsedAt = DateTime.UtcNow; }
}

public class AuditLog
{
    public long LogId { get; set; }
    public long? UserId { get; set; }
    public string? Email { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? FailureReason { get; set; }
    public string? AdditionalData { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
}

public static class AuditActions
{
    public const string LoginSuccess     = "LOGIN_SUCCESS";
    public const string LoginFailed      = "LOGIN_FAILED";
    public const string RegisterSuccess  = "REGISTER_SUCCESS";
    public const string Logout           = "LOGOUT";
    public const string TokenRefreshed   = "TOKEN_REFRESHED";
    public const string PasswordChanged  = "PASSWORD_CHANGED";
    public const string PasswordResetReq = "PASSWORD_RESET_REQ";
    public const string PasswordResetDone= "PASSWORD_RESET_DONE";
    public const string EmailVerified    = "EMAIL_VERIFIED";
    public const string RoleAssigned     = "ROLE_ASSIGNED";
}
