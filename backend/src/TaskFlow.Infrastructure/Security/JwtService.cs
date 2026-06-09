using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Security;

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

public class SecuritySettings
{
    public const string SectionName = "Security";
    public int  MaxFailedLoginAttempts   { get; init; } = 5;
    public int  LockoutDurationMinutes   { get; init; } = 15;
    public int  MaxActiveRefreshTokens   { get; init; } = 5;
    public int  BcryptWorkFactor         { get; init; } = 12;
    public bool RequireEmailVerification { get; init; } = false;
}

public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;
    private readonly byte[] _accessKey;

    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings  = settings.Value;
        _accessKey = Encoding.UTF8.GetBytes(_settings.AccessSecret);
        if (_accessKey.Length < 32)
            throw new InvalidOperationException("JWT AccessSecret must be at least 32 characters.");
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("uid",      user.Id.ToString()),
            new Claim("roleId",   user.GetPrimaryRoleId().ToString()),
            new Claim("roleName", user.GetPrimaryRoleName()),
            new Claim("name",     user.FullName),
        };

        var creds  = new SigningCredentials(new SymmetricSecurityKey(_accessKey), SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer, audience: _settings.Audience,
            claims: claims, notBefore: DateTime.UtcNow, expires: expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string RawToken, byte[] TokenHash) GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        var raw  = Convert.ToBase64String(bytes);
        return (raw, HashToken(raw));
    }

    public (string RawToken, byte[] TokenHash) GenerateOneTimeToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var raw = Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        return (raw, HashToken(raw));
    }

    public byte[] HashToken(string rawToken)
        => SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var params_ = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateIssuerSigningKey = true, ValidateLifetime = false,
            ValidIssuer = _settings.Issuer, ValidAudience = _settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(_accessKey),
            ClockSkew = TimeSpan.Zero,
        };
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, params_, out var validated);
            if (validated is not JwtSecurityToken jwt || !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                return null;
            return principal;
        }
        catch { return null; }
    }

    public long? GetUserIdFromToken(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            return long.TryParse(sub, out var id) ? id : null;
        }
        catch { return null; }
    }
}

public class PasswordHasher : IPasswordHasher
{
    private readonly int _workFactor;
    public PasswordHasher(IOptions<SecuritySettings> settings) => _workFactor = settings.Value.BcryptWorkFactor;

    public (byte[] Hash, byte[] Salt) HashPassword(string plainPassword)
    {
        var salt = BCrypt.Net.BCrypt.GenerateSalt(_workFactor);
        var hash = BCrypt.Net.BCrypt.HashPassword(plainPassword, salt);
        return (Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(salt));
    }

    public bool VerifyPassword(string plainPassword, byte[] storedHash, byte[] storedSalt)
    {
        var hashStr = Encoding.UTF8.GetString(storedHash);
        return BCrypt.Net.BCrypt.Verify(plainPassword, hashStr);
    }
}
