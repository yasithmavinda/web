using AutoMapper;
using TaskFlow.Domain.Entities;
using TaskFlow.Application.DTOs.Auth;
using TaskFlow.Application.DTOs.Tasks;
using TaskFlow.Application.DTOs.Projects;
using TaskFlow.Application.DTOs.Comments;
using TaskFlow.Application.DTOs.Notifications;

namespace TaskFlow.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ── User ──────────────────────────────────────────────
        CreateMap<User, UserDto>()
            .ForMember(d => d.UserId,   o => o.MapFrom(s => s.Id))
            .ForMember(d => d.RoleName, o => o.MapFrom(s => s.GetPrimaryRoleName()))
            .ForMember(d => d.RoleId,   o => o.MapFrom(s => s.GetPrimaryRoleId()));

        CreateMap<User, UserSummaryDto>()
            .ForMember(d => d.UserId,   o => o.MapFrom(s => s.Id))
            .ForMember(d => d.RoleName, o => o.MapFrom(s => s.GetPrimaryRoleName()))
            .ForMember(d => d.RoleId,   o => o.MapFrom(s => s.GetPrimaryRoleId()));

        // ── Project ───────────────────────────────────────────
        CreateMap<Project, ProjectDto>()
            .ForMember(d => d.ProjectId,           o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Status,              o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Priority,            o => o.MapFrom(s => s.Priority.ToString()))
            .ForMember(d => d.MemberCount,         o => o.MapFrom(s => s.Members.Count))
            .ForMember(d => d.TaskCount,           o => o.MapFrom(s => s.Tasks.Count(t => !t.IsArchived)))
            .ForMember(d => d.CompletedTaskCount,  o => o.MapFrom(s =>
                s.Tasks.Count(t => !t.IsArchived &&
                    t.Status == Domain.Enums.TaskStatus.Done)));

        CreateMap<ProjectMember, ProjectMemberDto>()
            .ForMember(d => d.MemberId,     o => o.MapFrom(s => s.MemberId))
            .ForMember(d => d.ProjectRole,  o => o.MapFrom(s => s.ProjectRole.ToString()));

        // ── Task ──────────────────────────────────────────────
        CreateMap<TaskItem, TaskDto>()
            .ForMember(d => d.TaskId,          o => o.MapFrom(s => s.Id))
            .ForMember(d => d.ProjectName,     o => o.MapFrom(s => s.Project.Name))
            .ForMember(d => d.Status,          o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Priority,        o => o.MapFrom(s => s.Priority.ToString()))
            .ForMember(d => d.Assignees,       o => o.MapFrom(s => s.Assignments.Select(a => a.AssignedTo)))
            .ForMember(d => d.Tags,            o => o.MapFrom(s => s.TaskTags.Select(tt => tt.Tag)))
            .ForMember(d => d.SubTaskCount,    o => o.MapFrom(s => s.SubTasks.Count))
            .ForMember(d => d.CommentCount,    o => o.MapFrom(s => s.Comments.Count(c => !c.IsDeleted)))
            .ForMember(d => d.AttachmentCount, o => o.MapFrom(s => s.Attachments.Count));

        CreateMap<Tag, TagDto>().ForMember(d => d.TagId, o => o.MapFrom(s => s.Id));

        CreateMap<TaskStatusHistory, TaskStatusHistoryDto>()
            .ForMember(d => d.OldStatus, o => o.MapFrom(s => s.OldStatus.ToString()))
            .ForMember(d => d.NewStatus, o => o.MapFrom(s => s.NewStatus.ToString()));

        CreateMap<Attachment, AttachmentDto>()
            .ForMember(d => d.AttachmentId,     o => o.MapFrom(s => s.Id))
            .ForMember(d => d.FileSizeFormatted, o => o.MapFrom(s => FormatFileSize(s.FileSize)));

        // ── Comment ───────────────────────────────────────────
        CreateMap<Comment, CommentDto>()
            .ForMember(d => d.CommentId,  o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Author,     o => o.MapFrom(s => s.User))
            .ForMember(d => d.ReplyCount, o => o.MapFrom(s => s.Replies.Count));

        // ── Notification ──────────────────────────────────────
        CreateMap<Notification, NotificationDto>()
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()));

        CreateMap<NotificationSetting, NotificationSettingDto>();
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1048576) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1073741824) return $"{bytes / 1048576.0:F1} MB";
        return $"{bytes / 1073741824.0:F1} GB";
    }
}
