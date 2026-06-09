using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class Role : BaseEntity
{
    public new byte Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserRole> UserRoles { get; set; } = [];
}

public class UserRole
{
    public long UserRoleId { get; set; }
    public long UserId { get; set; }
    public byte RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public long? AssignedBy { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
