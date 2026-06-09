namespace TaskFlow.Application.DTOs.Auth;

public record RegisterDto(string FullName, string Email, string Password, byte RoleId = 3);
public record LoginDto(string Email, string Password);
public record RefreshTokenDto(string RefreshToken);
public record ForgotPasswordDto(string Email);
public record ResetPasswordDto(string Token, string NewPassword, string ConfirmPassword);
public record ChangePasswordDto(string CurrentPassword, string NewPassword, string ConfirmPassword);

public class AuthResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserSummaryDto User { get; set; } = null!;
}

public class SessionDto
{
    public long SessionId { get; set; }
    public string DeviceInfo { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
