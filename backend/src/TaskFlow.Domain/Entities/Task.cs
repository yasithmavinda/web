using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;
using TaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Domain.Entities;

public class TaskItem : BaseEntity
{
    public long ProjectId { get; set; }
    public long CreatedBy { get; set; }
    public long? ParentTaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Backlog;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateOnly? DueDate { get; set; }
    public DateOnly? StartDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public decimal? ActualHours { get; set; }
    public byte? StoryPoints { get; set; }
    public int Position { get; set; } = 0;
    public bool IsArchived { get; set; } = false;

    public Project Project { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public TaskItem? ParentTask { get; set; }
    public ICollection<TaskItem> SubTasks { get; set; } = [];
    public ICollection<TaskAssignment> Assignments { get; set; } = [];
    public ICollection<TaskStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Attachment> Attachments { get; set; } = [];
    public ICollection<TaskTag> TaskTags { get; set; } = [];
}

public class TaskAssignment
{
    public long AssignmentId { get; set; }
    public long TaskId { get; set; }
    public long AssignedToUserId { get; set; }
    public long AssignedByUserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public TaskItem Task { get; set; } = null!;
    public User AssignedTo { get; set; } = null!;
    public User AssignedBy { get; set; } = null!;
}

public class TaskStatusHistory
{
    public long HistoryId { get; set; }
    public long TaskId { get; set; }
    public long ChangedBy { get; set; }
    public TaskStatus OldStatus { get; set; }
    public TaskStatus NewStatus { get; set; }
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public TaskItem Task { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}

public class Tag : BaseEntity
{
    public long ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366F1";

    public Project Project { get; set; } = null!;
    public ICollection<TaskTag> TaskTags { get; set; } = [];
}

public class TaskTag
{
    public long TaskId { get; set; }
    public long TagId { get; set; }

    public TaskItem Task { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
