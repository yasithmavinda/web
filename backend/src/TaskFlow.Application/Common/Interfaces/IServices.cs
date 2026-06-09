using System.Security.Claims;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    (string RawToken, byte[] TokenHash) GenerateRefreshToken();
    (string RawToken, byte[] TokenHash) GenerateOneTimeToken();
    byte[] HashToken(string rawToken);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    long? GetUserIdFromToken(string token);
}

public interface IPasswordHasher
{
    (byte[] Hash, byte[] Salt) HashPassword(string plainPassword);
    bool VerifyPassword(string plainPassword, byte[] storedHash, byte[] storedSalt);
}

public interface ICurrentUserService
{
    long? UserId { get; }
    string? Email { get; }
    string? RoleName { get; }
    byte? RoleId { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsProjectManager { get; }
}

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string fullName, string token);
    Task SendPasswordResetAsync(string toEmail, string fullName, string token);
    Task SendWelcomeEmailAsync(string toEmail, string fullName);
}
