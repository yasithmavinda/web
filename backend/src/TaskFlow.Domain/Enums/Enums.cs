namespace TaskFlow.Domain.Enums;

public enum TaskStatus  { Backlog = 0, Todo = 1, InProgress = 2, InReview = 3, Done = 4, Blocked = 5 }
public enum TaskPriority { Low = 0, Medium = 1, High = 2, Critical = 3 }
public enum ProjectStatus { Planning = 0, Active = 1, OnHold = 2, Completed = 3, Archived = 4 }
public enum ProjectRole  { Owner = 0, Manager = 1, Member = 2, Viewer = 3 }
public enum NotificationType
{
    TaskAssigned = 0, TaskStatusChanged = 1, CommentAdded = 2,
    TaskOverdue = 3, MentionedInComment = 4, ProjectInvite = 5
}
public enum SystemRole { Admin = 1, ProjectManager = 2, Collaborator = 3 }
