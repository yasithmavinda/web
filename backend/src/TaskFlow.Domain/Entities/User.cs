using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public byte[] PasswordHash { get; set; } = [];
    public byte[] PasswordSalt { get; set; } = [];
    public string? AvatarUrl { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerifyToken { get; set; }
    public DateTime? EmailVerifyExpiry { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Computed
    public bool IsLocked  => LockoutUntil.HasValue && LockoutUntil > DateTime.UtcNow;
    public bool IsDeleted => DeletedAt.HasValue;
    public bool CanLogin  => IsActive && !IsDeleted && !IsLocked;

    // Navigation
    public ICollection<UserRole>     UserRoles     { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = [];
    public ICollection<TaskAssignment> TaskAssignments { get; set; } = [];
    public ICollection<Comment>      Comments      { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public NotificationSetting?      NotificationSettings { get; set; }

    // Helpers
    public string GetPrimaryRoleName() 
    {
        var role = UserRoles.FirstOrDefault();
        if (role?.Role?.RoleName != null) return role.Role.RoleName;
        return role?.RoleId switch
        {
            1 => "Admin",
            2 => "ProjectManager",
            _ => "Collaborator"
        };
    }
    public byte   GetPrimaryRoleId()   => UserRoles.FirstOrDefault()?.RoleId ?? 3;

    public void IncrementFailedLogins()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
            LockoutUntil = DateTime.UtcNow.AddMinutes(15);
    }

    public void ResetFailedLogins()
    {
        FailedLoginAttempts = 0;
        LockoutUntil = null;
    }

    public void RecordLogin(string ip)
    {
        LastLoginAt = DateTime.UtcNow;
        LastLoginIp = ip;
        ResetFailedLogins();
    }
}
