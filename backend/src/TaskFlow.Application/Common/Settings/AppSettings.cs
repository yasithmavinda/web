namespace TaskFlow.Application.Common.Settings;

/// <summary>
/// JWT configuration — bound from appsettings.json "Jwt" section.
/// Duplicated here in Application layer to avoid circular dependency
/// between Application → Infrastructure.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";
    public string AccessSecret   { get; init; } = string.Empty;
    public string RefreshSecret  { get; init; } = string.Empty;
    public string Issuer         { get; init; } = string.Empty;
    public string Audience       { get; init; } = string.Empty;
    public int AccessTokenExpiryMinutes    { get; init; } = 15;
    public int RefreshTokenExpiryDays      { get; init; } = 7;
    public int ResetTokenExpiryHours       { get; init; } = 1;
    public int EmailVerifyTokenExpiryHours { get; init; } = 24;
}

/// <summary>
/// Security configuration — bound from appsettings.json "Security" section.
/// </summary>
public class SecuritySettings
{
    public const string SectionName = "Security";
    public int  MaxFailedLoginAttempts   { get; init; } = 5;
    public int  LockoutDurationMinutes   { get; init; } = 15;
    public int  MaxActiveRefreshTokens   { get; init; } = 5;
    public int  BcryptWorkFactor         { get; init; } = 12;
    public bool RequireEmailVerification { get; init; } = false;
}
