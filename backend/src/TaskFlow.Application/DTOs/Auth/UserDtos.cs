namespace TaskFlow.Application.DTOs.Auth;

public class UserDto
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public byte RoleId { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserSummaryDto
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public byte RoleId { get; set; }
}

public record UpdateProfileDto(string FullName, string? JobTitle, string? Department, string? Bio);
public record UpdateAvatarDto(string AvatarUrl);
public record ToggleUserStatusDto(bool IsActive, string? Reason);
public record AssignRoleDto(long UserId, byte RoleId);

public class UserStatsDto
{
    public int TotalAssignedTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int OverdueTasks { get; set; }
    public double CompletionRate { get; set; }
    public int TotalProjects { get; set; }
    public int TotalComments { get; set; }
}

public class WorkloadDto
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int TotalTasks { get; set; }
    public int TodoTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int DoneTasks { get; set; }
    public int OverdueTasks { get; set; }
}
