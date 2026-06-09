using TaskFlow.Application.DTOs.Auth;

namespace TaskFlow.Application.DTOs.Notifications;

public class NotificationDto
{
    public long NotificationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public UserSummaryDto? Actor { get; set; }
    public long? TaskId { get; set; }
    public long? ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationSettingDto
{
    public bool EmailOnTaskAssigned { get; set; }
    public bool EmailOnCommentAdded { get; set; }
    public bool EmailOnMentioned { get; set; }
    public bool EmailOnTaskOverdue { get; set; }
    public bool PushOnTaskAssigned { get; set; }
    public bool PushOnCommentAdded { get; set; }
    public bool InAppOnAll { get; set; }
}
