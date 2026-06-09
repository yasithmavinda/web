using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Common.Interfaces.Repositories;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.DTOs.Tasks;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using TaskStatus = TaskFlow.Domain.Enums.TaskStatus;
using TaskPriority = TaskFlow.Domain.Enums.TaskPriority;

namespace TaskFlow.Application.Services;

public interface ITaskService
{
    Task<TaskDto> GetByIdAsync(long id, long requestingUserId, CancellationToken ct = default);
    Task<PagedResult<TaskDto>> GetFilteredAsync(TaskFilterDto filter, CancellationToken ct = default);
    Task<TaskDto> CreateAsync(CreateTaskDto dto, long createdBy, CancellationToken ct = default);
    Task<TaskDto> UpdateAsync(long taskId, UpdateTaskDto dto, long userId, CancellationToken ct = default);
    Task<TaskDto> UpdateStatusAsync(long taskId, UpdateTaskStatusDto dto, long userId, CancellationToken ct = default);
    Task UpdatePositionAsync(long taskId, UpdateTaskPositionDto dto, long userId, CancellationToken ct = default);
    Task AssignUsersAsync(long taskId, AssignUsersDto dto, long assignedBy, CancellationToken ct = default);
    Task ArchiveAsync(long taskId, long userId, CancellationToken ct = default);
    Task DeleteAsync(long taskId, long userId, CancellationToken ct = default);
    Task<IEnumerable<TaskStatusHistoryDto>> GetStatusHistoryAsync(long taskId, CancellationToken ct = default);
}

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly INotificationRepository _notifRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<TaskService> _log;

    public TaskService(ITaskRepository taskRepo, IProjectRepository projectRepo,
        INotificationRepository notifRepo, IMapper mapper, ILogger<TaskService> log)
    {
        _taskRepo = taskRepo; _projectRepo = projectRepo;
        _notifRepo = notifRepo; _mapper = mapper; _log = log;
    }

    public async Task<TaskDto> GetByIdAsync(long id, long requestingUserId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdWithDetailsAsync(id, ct) ?? throw new NotFoundException("Task", id);
        if (!await _projectRepo.IsUserMemberAsync(task.ProjectId, requestingUserId, ct))
            throw new UnauthorizedException("You are not a member of this project.");
        return _mapper.Map<TaskDto>(task);
    }

    public async Task<PagedResult<TaskDto>> GetFilteredAsync(TaskFilterDto filter, CancellationToken ct = default)
    {
        var result = await _taskRepo.GetFilteredAsync(filter, ct);
        return new PagedResult<TaskDto>
        {
            Items = _mapper.Map<IEnumerable<TaskDto>>(result.Items),
            TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize,
        };
    }

    public async Task<TaskDto> CreateAsync(CreateTaskDto dto, long createdBy, CancellationToken ct = default)
    {
        if (!await _projectRepo.IsUserMemberAsync(dto.ProjectId, createdBy, ct))
            throw new UnauthorizedException("You are not a member of this project.");

        if (!Enum.TryParse<TaskStatus>(dto.Status, out var status))
            throw new BadRequestException($"Invalid status: {dto.Status}");
        if (!Enum.TryParse<TaskPriority>(dto.Priority, out var priority))
            throw new BadRequestException($"Invalid priority: {dto.Priority}");

        var task = new TaskItem
        {
            ProjectId = dto.ProjectId, CreatedBy = createdBy, ParentTaskId = dto.ParentTaskId,
            Title = dto.Title.Trim(), Description = dto.Description?.Trim(),
            Status = status, Priority = priority,
            DueDate = dto.DueDate, StartDate = dto.StartDate,
            EstimatedHours = dto.EstimatedHours, StoryPoints = dto.StoryPoints,
        };

        var created = await _taskRepo.CreateAsync(task, ct);

        // Assign users
        foreach (var uid in dto.AssigneeIds.Distinct())
        {
            await _taskRepo.AddAssignmentAsync(new TaskAssignment
            {
                TaskId = created.Id, AssignedToUserId = uid, AssignedByUserId = createdBy,
            }, ct);

            // Send notification to each assignee (except creator)
            if (uid != createdBy)
            {
                await _notifRepo.CreateAsync(new Notification
                {
                    RecipientId = uid, ActorId = createdBy, TaskId = created.Id,
                    ProjectId = dto.ProjectId, Type = Domain.Enums.NotificationType.TaskAssigned,
                    Title = "New Task Assigned",
                    Message = $"You have been assigned to \"{task.Title}\".",
                    ActionUrl = $"/tasks/{created.Id}",
                }, ct);
            }
        }

        _log.LogInformation("Task {Id} created by user {UserId} in project {ProjId}", created.Id, createdBy, dto.ProjectId);
        return await GetByIdAsync(created.Id, createdBy, ct);
    }

    public async Task<TaskDto> UpdateAsync(long taskId, UpdateTaskDto dto, long userId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct) ?? throw new NotFoundException("Task", taskId);
        if (!await _projectRepo.IsUserMemberAsync(task.ProjectId, userId, ct))
            throw new UnauthorizedException("Not a member of this project.");

        if (dto.Title != null) task.Title = dto.Title.Trim();
        if (dto.Description != null) task.Description = dto.Description.Trim();
        if (dto.Priority != null && Enum.TryParse<TaskPriority>(dto.Priority, out var pri)) task.Priority = pri;
        if (dto.DueDate.HasValue) task.DueDate = dto.DueDate;
        if (dto.StartDate.HasValue) task.StartDate = dto.StartDate;
        if (dto.EstimatedHours.HasValue) task.EstimatedHours = dto.EstimatedHours;
        if (dto.StoryPoints.HasValue) task.StoryPoints = dto.StoryPoints;
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepo.UpdateAsync(task, ct);
        return await GetByIdAsync(taskId, userId, ct);
    }

    public async Task<TaskDto> UpdateStatusAsync(long taskId, UpdateTaskStatusDto dto, long userId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct) ?? throw new NotFoundException("Task", taskId);
        if (!await _projectRepo.IsUserMemberAsync(task.ProjectId, userId, ct))
            throw new UnauthorizedException("Not a member of this project.");

        if (!Enum.TryParse<TaskStatus>(dto.Status, out var newStatus))
            throw new BadRequestException($"Invalid status: {dto.Status}");

        var oldStatus = task.Status;
        if (oldStatus == newStatus) return await GetByIdAsync(taskId, userId, ct);

        await _taskRepo.AddStatusHistoryAsync(new TaskStatusHistory
        {
            TaskId = taskId, ChangedBy = userId,
            OldStatus = oldStatus, NewStatus = newStatus, Note = dto.Note,
        }, ct);

        task.Status = newStatus; task.UpdatedAt = DateTime.UtcNow;
        await _taskRepo.UpdateAsync(task, ct);

        // Notify creator if someone else changes status
        if (task.CreatedBy != userId)
        {
            await _notifRepo.CreateAsync(new Notification
            {
                RecipientId = task.CreatedBy, ActorId = userId, TaskId = taskId,
                Type = Domain.Enums.NotificationType.TaskStatusChanged,
                Title = "Task Status Updated",
                Message = $"\"{task.Title}\" moved to {newStatus}.",
                ActionUrl = $"/tasks/{taskId}",
            }, ct);
        }

        _log.LogInformation("Task {Id} status: {Old} → {New} by user {UserId}", taskId, oldStatus, newStatus, userId);
        return await GetByIdAsync(taskId, userId, ct);
    }

    public async Task UpdatePositionAsync(long taskId, UpdateTaskPositionDto dto, long userId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct) ?? throw new NotFoundException("Task", taskId);
        if (!await _projectRepo.IsUserMemberAsync(task.ProjectId, userId, ct))
            throw new UnauthorizedException("Not a member of this project.");

        task.Position  = dto.Position;
        task.UpdatedAt = DateTime.UtcNow;
        if (dto.Status != null && Enum.TryParse<TaskStatus>(dto.Status, out var s)) task.Status = s;
        await _taskRepo.UpdateAsync(task, ct);
    }

    public async Task AssignUsersAsync(long taskId, AssignUsersDto dto, long assignedBy, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdWithDetailsAsync(taskId, ct) ?? throw new NotFoundException("Task", taskId);
        if (!await _projectRepo.IsUserMemberAsync(task.ProjectId, assignedBy, ct))
            throw new UnauthorizedException("Not a member of this project.");

        // Remove existing assignments
        foreach (var a in task.Assignments.ToList())
            await _taskRepo.RemoveAssignmentAsync(taskId, a.AssignedToUserId, ct);

        // Add new ones
        foreach (var uid in dto.UserIds.Distinct())
        {
            await _taskRepo.AddAssignmentAsync(new TaskAssignment
            {
                TaskId = taskId, AssignedToUserId = uid, AssignedByUserId = assignedBy,
            }, ct);

            if (uid != assignedBy)
            {
                await _notifRepo.CreateAsync(new Notification
                {
                    RecipientId = uid, ActorId = assignedBy, TaskId = taskId,
                    Type = Domain.Enums.NotificationType.TaskAssigned,
                    Title = "Task Assigned", Message = $"You were assigned to \"{task.Title}\".",
                    ActionUrl = $"/tasks/{taskId}",
                }, ct);
            }
        }
    }

    public async Task ArchiveAsync(long taskId, long userId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct) ?? throw new NotFoundException("Task", taskId);
        if (!await _projectRepo.IsUserMemberAsync(task.ProjectId, userId, ct))
            throw new UnauthorizedException("Not a member of this project.");
        task.IsArchived = true; task.UpdatedAt = DateTime.UtcNow;
        await _taskRepo.UpdateAsync(task, ct);
    }

    public async Task DeleteAsync(long taskId, long userId, CancellationToken ct = default)
    {
        var task = await _taskRepo.GetByIdAsync(taskId, ct) ?? throw new NotFoundException("Task", taskId);
        if (!await _projectRepo.IsUserMemberAsync(task.ProjectId, userId, ct))
            throw new UnauthorizedException("Not a member of this project.");
        task.IsArchived = true; task.UpdatedAt = DateTime.UtcNow; // Soft delete via archive
        await _taskRepo.UpdateAsync(task, ct);
        _log.LogInformation("Task {Id} deleted by user {UserId}", taskId, userId);
    }

    public async Task<IEnumerable<TaskStatusHistoryDto>> GetStatusHistoryAsync(long taskId, CancellationToken ct = default)
    {
        var history = await _taskRepo.GetStatusHistoryAsync(taskId, ct);
        return _mapper.Map<IEnumerable<TaskStatusHistoryDto>>(history);
    }
}
