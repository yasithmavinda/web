using TaskFlow.Application.DTOs.Auth;

namespace TaskFlow.Application.DTOs.Comments;

public class CommentDto
{
    public long CommentId { get; set; }
    public long TaskId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }
    public long? ParentCommentId { get; set; }
    public UserSummaryDto Author { get; set; } = null!;
    public List<CommentDto> Replies { get; set; } = [];
    public int ReplyCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateCommentDto { public long TaskId { get; set; } public string Body { get; set; } = string.Empty; public long? ParentCommentId { get; set; } }
public class UpdateCommentDto { public string Body { get; set; } = string.Empty; }
