using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

public class Project : BaseEntity
{
    public long OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string ColorTag { get; set; } = "#6366F1";
    public string? CoverImageUrl { get; set; }
    public bool IsArchived { get; set; } = false;

    public User Owner { get; set; } = null!;
    public ICollection<ProjectMember> Members { get; set; } = [];
    public ICollection<TaskItem> Tasks { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
}

public class ProjectMember
{
    public long MemberId { get; set; }
    public long ProjectId { get; set; }
    public long UserId { get; set; }
    public ProjectRole ProjectRole { get; set; } = ProjectRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public long? InvitedBy { get; set; }

    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
}
