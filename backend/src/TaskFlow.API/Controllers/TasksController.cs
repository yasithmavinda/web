using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TaskFlow.API.Hubs;
using TaskFlow.Application.DTOs.Tasks;
using TaskFlow.Application.Services;

namespace TaskFlow.API.Controllers;

/// <summary>Tasks — Full CRUD, status tracking, assignments, Kanban positioning</summary>
[Authorize]
[Tags("Tasks")]
public class TasksController : BaseApiController
{
    private readonly ITaskService _taskSvc;
    private readonly IHubContext<NotificationHub> _hub;

    public TasksController(ITaskService taskSvc, IHubContext<NotificationHub> hub)
    { _taskSvc = taskSvc; _hub = hub; }

    /// <summary>Get tasks with filters. Members only see their projects' tasks.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] long? projectId, [FromQuery] string? status, [FromQuery] string? priority,
        [FromQuery] long? assigneeId, [FromQuery] string? search, [FromQuery] bool? isOverdue,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "CreatedAt", [FromQuery] string sortOrder = "DESC",
        CancellationToken ct = default)
    {
        var filter = new TaskFilterDto
        {
            ProjectId = projectId, Status = status, Priority = priority,
            AssigneeId = assigneeId, Search = search, IsOverdue = isOverdue,
            Page = page, PageSize = pageSize, SortBy = sortBy, SortOrder = sortOrder,
            RequestingUserId = CurrentUserId!.Value,
        };
        var result = await _taskSvc.GetFilteredAsync(filter, ct);
        return OkResponse(result);
    }

    /// <summary>Get a task by ID with full details (assignees, tags, sub-tasks, comments count).</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var task = await _taskSvc.GetByIdAsync(id, CurrentUserId!.Value, ct);
        return OkResponse(task);
    }

    /// <summary>Create a new task. ProjectManager or Admin only.</summary>
    [HttpPost]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto, [FromServices] FluentValidation.IValidator<CreateTaskDto> validator, CancellationToken ct)
    {
        var valResult = await validator.ValidateAsync(dto, ct);
        if (!valResult.IsValid)
            throw new FluentValidation.ValidationException(valResult.Errors);

        var task = await _taskSvc.CreateAsync(dto, CurrentUserId!.Value, ct);
        // Broadcast real-time update to all project members
        await _hub.Clients.Group($"project-{dto.ProjectId}").SendAsync("TaskCreated", task, ct);
        return CreatedResponse(task, "Task created.");
    }

    /// <summary>Update task details. ProjectManager or Admin only.</summary>
    [HttpPut("{id:long}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateTaskDto dto, CancellationToken ct)
    {
        var task = await _taskSvc.UpdateAsync(id, dto, CurrentUserId!.Value, ct);
        await _hub.Clients.Group($"project-{task.ProjectId}").SendAsync("TaskUpdated", task, ct);
        return OkResponse(task, "Task updated.");
    }

    /// <summary>Update task status. Any authenticated project member can do this.</summary>
    [HttpPatch("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateTaskStatusDto dto, CancellationToken ct)
    {
        var task = await _taskSvc.UpdateStatusAsync(id, dto, CurrentUserId!.Value, ct);
        await _hub.Clients.Group($"project-{task.ProjectId}").SendAsync("TaskStatusChanged", task, ct);
        return OkResponse(task, "Status updated.");
    }

    /// <summary>Update task Kanban board position (drag and drop).</summary>
    [HttpPatch("{id:long}/position")]
    public async Task<IActionResult> UpdatePosition(long id, [FromBody] UpdateTaskPositionDto dto, CancellationToken ct)
    {
        await _taskSvc.UpdatePositionAsync(id, dto, CurrentUserId!.Value, ct);
        return OkNoData("Position updated.");
    }

    /// <summary>Assign users to a task. Replaces all existing assignments.</summary>
    [HttpPut("{id:long}/assignees")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> AssignUsers(long id, [FromBody] AssignUsersDto dto, CancellationToken ct)
    {
        await _taskSvc.AssignUsersAsync(id, dto, CurrentUserId!.Value, ct);
        return OkNoData("Assignees updated.");
    }

    /// <summary>Archive a task (soft delete).</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> Archive(long id, CancellationToken ct)
    {
        await _taskSvc.ArchiveAsync(id, CurrentUserId!.Value, ct);
        return OkNoData("Task archived.");
    }

    /// <summary>Get the status change history for a task.</summary>
    [HttpGet("{id:long}/history")]
    public async Task<IActionResult> GetHistory(long id, CancellationToken ct)
    {
        var history = await _taskSvc.GetStatusHistoryAsync(id, ct);
        return OkResponse(history);
    }
}
