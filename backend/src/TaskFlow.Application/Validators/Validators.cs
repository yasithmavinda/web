using FluentValidation;
using TaskFlow.Application.DTOs.Auth;
using TaskFlow.Application.DTOs.Tasks;
using TaskFlow.Application.DTOs.Projects;
using TaskFlow.Application.DTOs.Comments;

namespace TaskFlow.Application.Validators;

// ── Auth Validators ──────────────────────────────────────────
public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MinimumLength(3).WithMessage("Full name must be at least 3 characters.")
            .MaximumLength(150).WithMessage("Full name cannot exceed 150 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Please enter a valid email address.")
            .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[@$!%*?&]").WithMessage("Password must contain at least one special character (@$!%*?&).");

        RuleFor(x => x.RoleId)
            .InclusiveBetween((byte)1, (byte)3)
            .WithMessage("Role ID must be 1 (Admin), 2 (ProjectManager), or 3 (Collaborator).");
    }
}

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]").Matches("[@$!%*?&]");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]").Matches("[@$!%*?&]");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

// ── Task Validators ──────────────────────────────────────────
public class CreateTaskValidator : AbstractValidator<CreateTaskDto>
{
    private static readonly string[] ValidStatuses   = ["Backlog", "Todo", "InProgress", "InReview", "Done", "Blocked"];
    private static readonly string[] ValidPriorities = ["Low", "Medium", "High", "Critical"];
    private static readonly byte[]   ValidStoryPoints = [1, 2, 3, 5, 8, 13, 21];

    public CreateTaskValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0).WithMessage("Project ID is required.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(10000).When(x => x.Description != null);
        RuleFor(x => x.Status).Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
        RuleFor(x => x.Priority).Must(p => ValidPriorities.Contains(p))
            .WithMessage($"Priority must be one of: {string.Join(", ", ValidPriorities)}.");
        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Due date cannot be in the past.")
            .When(x => x.DueDate.HasValue);
        RuleFor(x => x.EstimatedHours).InclusiveBetween(0, 9999m).When(x => x.EstimatedHours.HasValue);
        RuleFor(x => x.StoryPoints)
            .Must(sp => sp.HasValue && ValidStoryPoints.Contains(sp.Value))
            .WithMessage("Story points must be Fibonacci: 1, 2, 3, 5, 8, 13, or 21.")
            .When(x => x.StoryPoints.HasValue);
        RuleFor(x => x.AssigneeIds).Must(ids => ids.Count <= 20).WithMessage("Max 20 assignees.");
        RuleFor(x => x.TagIds).Must(ids => ids.Count <= 10).WithMessage("Max 10 tags.");
    }
}

public class UpdateTaskValidator : AbstractValidator<UpdateTaskDto>
{
    private static readonly string[] ValidPriorities = ["Low", "Medium", "High", "Critical"];
    public UpdateTaskValidator()
    {
        RuleFor(x => x.Title).MaximumLength(500).When(x => x.Title != null);
        RuleFor(x => x.Description).MaximumLength(10000).When(x => x.Description != null);
        RuleFor(x => x.Priority).Must(p => ValidPriorities.Contains(p!))
            .WithMessage($"Priority must be one of: {string.Join(", ", ValidPriorities)}.")
            .When(x => x.Priority != null);
        RuleFor(x => x.EstimatedHours).InclusiveBetween(0, 9999m).When(x => x.EstimatedHours.HasValue);
    }
}

public class UpdateTaskStatusValidator : AbstractValidator<UpdateTaskStatusDto>
{
    private static readonly string[] ValidStatuses = ["Backlog", "Todo", "InProgress", "InReview", "Done", "Blocked"];
    public UpdateTaskStatusValidator()
    {
        RuleFor(x => x.Status).NotEmpty()
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note != null);
    }
}

// ── Project Validators ──────────────────────────────────────────
public class CreateProjectValidator : AbstractValidator<CreateProjectDto>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description != null);
        RuleFor(x => x.ColorTag).Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Color must be a valid hex color (e.g. #6366F1).").When(x => x.ColorTag != null);
    }
}

// ── Comment Validators ──────────────────────────────────────────
public class CreateCommentValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentValidator()
    {
        RuleFor(x => x.TaskId).GreaterThan(0);
        RuleFor(x => x.Body).NotEmpty().MinimumLength(1).MaximumLength(5000);
    }
}

public class UpdateCommentValidator : AbstractValidator<UpdateCommentDto>
{
    public UpdateCommentValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MinimumLength(1).MaximumLength(5000);
    }
}
