using TaskFlow.Domain.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

public class Comment : BaseEntity
{
    public long TaskId { get; set; }
    public long UserId { get; set; }
    public long? ParentCommentId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsEdited { get; set; } = false;
    public bool IsDeleted { get; set; } = false;

    public TaskItem Task { get; set; } = null!;
    public User User { get; set; } = null!;
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = [];
    public ICollection<Attachment> Attachments { get; set; } = [];
}

public class Attachment : BaseEntity
{
    public long? TaskId { get; set; }
    public long? CommentId { get; set; }
    public long UploadedBy { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string StorageUrl { get; set; } = string.Empty;

    public TaskItem? Task { get; set; }
    public Comment? Comment { get; set; }
    public User Uploader { get; set; } = null!;
}

public class Notification
{
    public long NotificationId { get; set; }
    public long RecipientId { get; set; }
    public long? ActorId { get; set; }
    public long? TaskId { get; set; }
    public long? ProjectId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Recipient { get; set; } = null!;
    public User? Actor { get; set; }
    public TaskItem? Task { get; set; }
    public Project? Project { get; set; }
}

public class NotificationSetting
{
    public long SettingId { get; set; }
    public long UserId { get; set; }
    public bool EmailOnTaskAssigned { get; set; } = true;
    public bool EmailOnCommentAdded { get; set; } = true;
    public bool EmailOnMentioned { get; set; } = true;
    public bool EmailOnTaskOverdue { get; set; } = true;
    public bool PushOnTaskAssigned { get; set; } = true;
    public bool PushOnCommentAdded { get; set; } = true;
    public bool InAppOnAll { get; set; } = true;

    public User User { get; set; } = null!;
}
