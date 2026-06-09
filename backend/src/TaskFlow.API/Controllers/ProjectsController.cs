using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs.Projects;
using TaskFlow.Application.Services;

namespace TaskFlow.API.Controllers;

/// <summary>Projects — CRUD, member management</summary>
[Authorize]
[Tags("Projects")]
public class ProjectsController : BaseApiController
{
    private readonly IProjectService _projectSvc;
    public ProjectsController(IProjectService projectSvc) => _projectSvc = projectSvc;

    /// <summary>Get all projects the current user is a member of.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await _projectSvc.GetAllAsync(CurrentUserId!.Value, page, pageSize, ct);
        return OkResponse(result);
    }

    /// <summary>Get a single project by ID.</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var project = await _projectSvc.GetByIdAsync(id, CurrentUserId!.Value, ct);
        return OkResponse(project);
    }

    /// <summary>Create a new project. ProjectManager or Admin only.</summary>
    [HttpPost]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> Create([FromBody] CreateProjectDto dto, CancellationToken ct)
    {
        var project = await _projectSvc.CreateAsync(dto, CurrentUserId!.Value, ct);
        return CreatedResponse(project, "Project created.");
    }

    /// <summary>Update project details. ProjectManager or Admin only.</summary>
    [HttpPut("{id:long}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProjectDto dto, CancellationToken ct)
    {
        var project = await _projectSvc.UpdateAsync(id, dto, CurrentUserId!.Value, ct);
        return OkResponse(project, "Project updated.");
    }

    /// <summary>Archive a project. Owner only.</summary>
    [HttpDelete("{id:long}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> Archive(long id, CancellationToken ct)
    {
        await _projectSvc.ArchiveAsync(id, CurrentUserId!.Value, ct);
        return OkNoData("Project archived.");
    }

    /// <summary>Get all members of a project.</summary>
    [HttpGet("{id:long}/members")]
    public async Task<IActionResult> GetMembers(long id, CancellationToken ct)
    {
        var members = await _projectSvc.GetMembersAsync(id, CurrentUserId!.Value, ct);
        return OkResponse(members);
    }

    /// <summary>Add a member to a project. ProjectManager or Admin only.</summary>
    [HttpPost("{id:long}/members")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> AddMember(long id, [FromBody] AddProjectMemberDto dto, CancellationToken ct)
    {
        await _projectSvc.AddMemberAsync(id, dto, CurrentUserId!.Value, ct);
        return OkNoData("Member added to project.");
    }

    /// <summary>Remove a member from a project.</summary>
    [HttpDelete("{id:long}/members/{userId:long}")]
    [Authorize(Policy = "RequireManager")]
    public async Task<IActionResult> RemoveMember(long id, long userId, CancellationToken ct)
    {
        await _projectSvc.RemoveMemberAsync(id, userId, CurrentUserId!.Value, ct);
        return OkNoData("Member removed from project.");
    }
}
