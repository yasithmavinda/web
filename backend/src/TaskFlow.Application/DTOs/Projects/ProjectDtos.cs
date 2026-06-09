using TaskFlow.Application.DTOs.Auth;

namespace TaskFlow.Application.DTOs.Projects;

public class ProjectDto
{
    public long ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string ColorTag { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public bool IsArchived { get; set; }
    public UserSummaryDto Owner { get; set; } = null!;
    public int MemberCount { get; set; }
    public int TaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProjectMemberDto
{
    public long MemberId { get; set; }
    public UserSummaryDto User { get; set; } = null!;
    public string ProjectRole { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class CreateProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Priority { get; set; } = "Medium";
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string ColorTag { get; set; } = "#6366F1";
    public List<long> MemberIds { get; set; } = [];
}

public class UpdateProjectDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? ColorTag { get; set; }
}

public class AddProjectMemberDto { public long UserId { get; set; } public string ProjectRole { get; set; } = "Member"; }
