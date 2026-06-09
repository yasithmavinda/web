using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common.Interfaces.Repositories;
using TaskFlow.Application.Common.Models;
using TaskFlow.Application.DTOs.Projects;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;

namespace TaskFlow.Application.Services;

public interface IProjectService
{
    Task<ProjectDto> GetByIdAsync(long id, long requestingUserId, CancellationToken ct = default);
    Task<PagedResult<ProjectDto>> GetAllAsync(long userId, int page, int pageSize, CancellationToken ct = default);
    Task<ProjectDto> CreateAsync(CreateProjectDto dto, long ownerId, CancellationToken ct = default);
    Task<ProjectDto> UpdateAsync(long projectId, UpdateProjectDto dto, long userId, CancellationToken ct = default);
    Task ArchiveAsync(long projectId, long userId, CancellationToken ct = default);
    Task<IEnumerable<ProjectMemberDto>> GetMembersAsync(long projectId, long requestingUserId, CancellationToken ct = default);
    Task AddMemberAsync(long projectId, AddProjectMemberDto dto, long addedBy, CancellationToken ct = default);
    Task RemoveMemberAsync(long projectId, long userId, long removedBy, CancellationToken ct = default);
}

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<ProjectService> _log;

    public ProjectService(IProjectRepository projectRepo, IMapper mapper, ILogger<ProjectService> log)
    { _projectRepo = projectRepo; _mapper = mapper; _log = log; }

    public async Task<ProjectDto> GetByIdAsync(long id, long requestingUserId, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdWithMembersAsync(id, ct) ?? throw new NotFoundException("Project", id);
        if (!await _projectRepo.IsUserMemberAsync(id, requestingUserId, ct))
            throw new UnauthorizedException("You are not a member of this project.");
        return _mapper.Map<ProjectDto>(project);
    }

    public async Task<PagedResult<ProjectDto>> GetAllAsync(long userId, int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _projectRepo.GetAllAsync(userId, page, pageSize, ct);
        return new PagedResult<ProjectDto>
        {
            Items = _mapper.Map<IEnumerable<ProjectDto>>(result.Items),
            TotalCount = result.TotalCount, Page = result.Page, PageSize = result.PageSize,
        };
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto, long ownerId, CancellationToken ct = default)
    {
        if (!Enum.TryParse<TaskPriority>(dto.Priority, out var priority))
            throw new BadRequestException($"Invalid priority: {dto.Priority}");

        var project = new Project
        {
            OwnerId = ownerId, Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(), Priority = priority,
            StartDate = dto.StartDate, EndDate = dto.EndDate,
            ColorTag = dto.ColorTag,
        };

        // Add owner as a member automatically
        project.Members.Add(new ProjectMember
        {
            UserId = ownerId, ProjectRole = ProjectRole.Owner,
        });

        // Add specified members
        foreach (var uid in dto.MemberIds.Distinct().Where(id => id != ownerId))
        {
            project.Members.Add(new ProjectMember
            {
                UserId = uid, ProjectRole = ProjectRole.Member, InvitedBy = ownerId,
            });
        }

        var created = await _projectRepo.CreateAsync(project, ct);
        _log.LogInformation("Project {Id} created by user {UserId}", created.Id, ownerId);
        return _mapper.Map<ProjectDto>(created);
    }

    public async Task<ProjectDto> UpdateAsync(long projectId, UpdateProjectDto dto, long userId, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct) ?? throw new NotFoundException("Project", projectId);
        if (!await _projectRepo.IsUserMemberAsync(projectId, userId, ct))
            throw new UnauthorizedException("Not a member of this project.");

        if (dto.Name != null) project.Name = dto.Name.Trim();
        if (dto.Description != null) project.Description = dto.Description.Trim();
        if (dto.ColorTag != null) project.ColorTag = dto.ColorTag;
        if (dto.StartDate.HasValue) project.StartDate = dto.StartDate;
        if (dto.EndDate.HasValue) project.EndDate = dto.EndDate;
        if (dto.Status != null && Enum.TryParse<ProjectStatus>(dto.Status, out var s)) project.Status = s;
        if (dto.Priority != null && Enum.TryParse<TaskPriority>(dto.Priority, out var p)) project.Priority = p;
        project.UpdatedAt = DateTime.UtcNow;

        await _projectRepo.UpdateAsync(project, ct);
        return _mapper.Map<ProjectDto>(project);
    }

    public async Task ArchiveAsync(long projectId, long userId, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct) ?? throw new NotFoundException("Project", projectId);
        if (project.OwnerId != userId) throw new UnauthorizedException("Only the project owner can archive it.");
        project.IsArchived = true; project.UpdatedAt = DateTime.UtcNow;
        await _projectRepo.UpdateAsync(project, ct);
    }

    public async Task<IEnumerable<ProjectMemberDto>> GetMembersAsync(long projectId, long requestingUserId, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdWithMembersAsync(projectId, ct) ?? throw new NotFoundException("Project", projectId);
        if (!await _projectRepo.IsUserMemberAsync(projectId, requestingUserId, ct))
            throw new UnauthorizedException("Not a member of this project.");
        return _mapper.Map<IEnumerable<ProjectMemberDto>>(project.Members);
    }

    public async Task AddMemberAsync(long projectId, AddProjectMemberDto dto, long addedBy, CancellationToken ct = default)
    {
        if (!await _projectRepo.IsUserMemberAsync(projectId, addedBy, ct))
            throw new UnauthorizedException("Not a member of this project.");

        if (await _projectRepo.IsUserMemberAsync(projectId, dto.UserId, ct))
            throw new ConflictException("User is already a member of this project.");

        if (!Enum.TryParse<ProjectRole>(dto.ProjectRole, out var role))
            throw new BadRequestException($"Invalid project role: {dto.ProjectRole}");

        await _projectRepo.AddMemberAsync(new ProjectMember
        {
            ProjectId = projectId, UserId = dto.UserId,
            ProjectRole = role, InvitedBy = addedBy,
        }, ct);
    }

    public async Task RemoveMemberAsync(long projectId, long userId, long removedBy, CancellationToken ct = default)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct) ?? throw new NotFoundException("Project", projectId);
        if (project.OwnerId != removedBy && removedBy != userId)
            throw new UnauthorizedException("Only the project owner can remove members.");
        if (userId == project.OwnerId)
            throw new BadRequestException("Cannot remove the project owner.");
        await _projectRepo.RemoveMemberAsync(projectId, userId, ct);
    }
}
