using TaskFlow.Application.DTOs.Auth;

namespace TaskFlow.Application.DTOs.Tasks;

public class TaskDto
{
    public long TaskId { get; set; }
    public long ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public DateOnly? StartDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public byte? StoryPoints { get; set; }
    public int Position { get; set; }
    public bool IsArchived { get; set; }
    public long? ParentTaskId { get; set; }
    public int SubTaskCount { get; set; }
    public int CommentCount { get; set; }
    public int AttachmentCount { get; set; }
    public UserSummaryDto CreatedBy { get; set; } = null!;
    public List<UserSummaryDto> Assignees { get; set; } = [];
    public List<TagDto> Tags { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TagDto { public long TagId { get; set; } public string Name { get; set; } = string.Empty; public string Color { get; set; } = string.Empty; }

public class CreateTaskDto
{
    public long ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Backlog";
    public string Priority { get; set; } = "Medium";
    public DateOnly? DueDate { get; set; }
    public DateOnly? StartDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public byte? StoryPoints { get; set; }
    public long? ParentTaskId { get; set; }
    public List<long> AssigneeIds { get; set; } = [];
    public List<long> TagIds { get; set; } = [];
}

public class UpdateTaskDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? StartDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public byte? StoryPoints { get; set; }
}

public class UpdateTaskStatusDto { public string Status { get; set; } = string.Empty; public string? Note { get; set; } }
public class UpdateTaskPositionDto { public int Position { get; set; } public string? Status { get; set; } }
public class AssignUsersDto { public List<long> UserIds { get; set; } = []; }
public class AddTagDto { public long TagId { get; set; } }

public class TaskFilterDto
{
    public long? ProjectId { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public long? AssigneeId { get; set; }
    public string? Search { get; set; }
    public bool? IsOverdue { get; set; }
    public long? ParentTaskId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "DESC";
    public long RequestingUserId { get; set; }
}

public class TaskStatusHistoryDto
{
    public long HistoryId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public UserSummaryDto ChangedBy { get; set; } = null!;
    public DateTime ChangedAt { get; set; }
}

public class AttachmentDto
{
    public long AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string StorageUrl { get; set; } = string.Empty;
    public UserSummaryDto UploadedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class CreateAttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string StorageUrl { get; set; } = string.Empty;
}
