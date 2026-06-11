using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Infrastructure.Persistence;

public class TaskFlowDbContext : DbContext
{
    public TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options) : base(options) { }

    // ── DbSets ───────────────────────────────────────────────
    public DbSet<User>                 Users                 => Set<User>();
    public DbSet<Role>                 Roles                 => Set<Role>();
    public DbSet<UserRole>             UserRoles             => Set<UserRole>();
    public DbSet<RefreshToken>         RefreshTokens         => Set<RefreshToken>();
    public DbSet<PasswordResetToken>   PasswordResetTokens   => Set<PasswordResetToken>();
    public DbSet<AuditLog>             AuditLogs             => Set<AuditLog>();
    public DbSet<Project>              Projects              => Set<Project>();
    public DbSet<ProjectMember>        ProjectMembers        => Set<ProjectMember>();
    public DbSet<TaskItem>             Tasks                 => Set<TaskItem>();
    public DbSet<TaskAssignment>       TaskAssignments       => Set<TaskAssignment>();
    public DbSet<TaskStatusHistory>    TaskStatusHistories   => Set<TaskStatusHistory>();
    public DbSet<Tag>                  Tags                  => Set<Tag>();
    public DbSet<TaskTag>              TaskTags              => Set<TaskTag>();
    public DbSet<Comment>              Comments              => Set<Comment>();
    public DbSet<Attachment>           Attachments           => Set<Attachment>();
    public DbSet<Notification>         Notifications         => Set<Notification>();
    public DbSet<NotificationSetting>  NotificationSettings  => Set<NotificationSetting>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ── User ─────────────────────────────────────────────
        b.Entity<User>(e =>
        {
            e.ToTable("Users", "auth");
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasColumnName("UserId").ValueGeneratedOnAdd();
            e.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            e.Property(u => u.Email).IsRequired().HasMaxLength(254);
            e.Property(u => u.PasswordHash).IsRequired().HasMaxLength(64);
            e.Property(u => u.PasswordSalt).IsRequired().HasMaxLength(32);
            e.Property(u => u.AvatarUrl).HasMaxLength(500);
            e.Property(u => u.JobTitle).HasMaxLength(100);
            e.Property(u => u.Department).HasMaxLength(100);
            e.Property(u => u.Bio).HasMaxLength(500);
            e.Property(u => u.LastLoginIp).HasMaxLength(45);
            e.Property(u => u.EmailVerifyToken).HasMaxLength(128);
            e.HasIndex(u => u.Email).IsUnique().HasFilter("[DeletedAt] IS NULL");
            e.Ignore(u => u.IsLocked);
            e.Ignore(u => u.IsDeleted);
            e.Ignore(u => u.CanLogin);
        });

        // ── Role ──────────────────────────────────────────────
        b.Entity<Role>(e =>
        {
            e.ToTable("Roles", "auth");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("RoleId");
            e.Property(r => r.RoleName).IsRequired().HasMaxLength(50);
            e.Property(r => r.Description).HasMaxLength(200);
            e.HasIndex(r => r.RoleName).IsUnique();
            // Seed data
            e.HasData(
                new Role { Id = 1, RoleName = "Admin", Description = "Full system access." },
                new Role { Id = 2, RoleName = "ProjectManager", Description = "Manage projects and tasks." },
                new Role { Id = 3, RoleName = "Collaborator", Description = "Work on assigned tasks." }
            );
        });

        // ── UserRole ──────────────────────────────────────────
        b.Entity<UserRole>(e =>
        {
            e.ToTable("UserRoles", "auth");
            e.HasKey(ur => ur.UserRoleId);
            e.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
            e.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── RefreshToken ──────────────────────────────────────
        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens", "auth");
            e.HasKey(t => t.TokenId);
            e.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
            e.Property(t => t.DeviceInfo).HasMaxLength(500);
            e.Property(t => t.IpAddress).HasMaxLength(45);
            e.Property(t => t.RevokedReason).HasMaxLength(200);
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasIndex(t => t.UserId).HasFilter("[IsRevoked] = 0");
            e.HasOne(t => t.User).WithMany(u => u.RefreshTokens).HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Ignore(t => t.IsExpired);
            e.Ignore(t => t.IsActive);
        });

        // ── PasswordResetToken ────────────────────────────────
        b.Entity<PasswordResetToken>(e =>
        {
            e.ToTable("PasswordResetTokens", "auth");
            e.HasKey(t => t.TokenId);
            e.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
            e.Property(t => t.IpAddress).HasMaxLength(45);
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Ignore(t => t.IsExpired);
            e.Ignore(t => t.IsValid);
        });

        // ── AuditLog ──────────────────────────────────────────
        b.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs", "security");
            e.HasKey(a => a.LogId);
            e.Property(a => a.Action).IsRequired().HasMaxLength(100);
            e.Property(a => a.Email).HasMaxLength(254);
            e.Property(a => a.IpAddress).HasMaxLength(45);
            e.Property(a => a.UserAgent).HasMaxLength(500);
            e.Property(a => a.FailureReason).HasMaxLength(500);
            e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(a => new { a.UserId, a.CreatedAt });
            e.HasIndex(a => new { a.Action, a.CreatedAt });
        });

        // ── Project ───────────────────────────────────────────
        b.Entity<Project>(e =>
        {
            e.ToTable("Projects");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("ProjectId").ValueGeneratedOnAdd();
            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
            e.Property(p => p.Description).HasMaxLength(2000);
            e.Property(p => p.ColorTag).HasMaxLength(7).HasDefaultValue("#6366F1");
            e.Property(p => p.CoverImageUrl).HasMaxLength(500);
            e.Property(p => p.Status).HasConversion<string>();
            e.Property(p => p.Priority).HasConversion<string>();
            e.HasOne(p => p.Owner).WithMany().HasForeignKey(p => p.OwnerId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(p => p.Members).WithOne(m => m.Project).HasForeignKey(m => m.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Tasks).WithOne(t => t.Project).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── ProjectMember ─────────────────────────────────────
        b.Entity<ProjectMember>(e =>
        {
            e.ToTable("ProjectMembers");
            e.HasKey(m => m.MemberId);
            e.HasIndex(m => new { m.ProjectId, m.UserId }).IsUnique();
            e.Property(m => m.ProjectRole).HasConversion<string>();
            e.HasOne(m => m.User).WithMany(u => u.ProjectMemberships).HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── TaskItem ──────────────────────────────────────────
        b.Entity<TaskItem>(e =>
        {
            e.ToTable("Tasks");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("TaskId").ValueGeneratedOnAdd();
            e.Property(t => t.Title).IsRequired().HasMaxLength(500);
            e.Property(t => t.Description).HasMaxLength(10000);
            e.Property(t => t.Status).HasConversion<string>();
            e.Property(t => t.Priority).HasConversion<string>();
            e.Property(t => t.EstimatedHours).HasColumnType("decimal(6,2)");
            e.Property(t => t.ActualHours).HasColumnType("decimal(6,2)");
            e.HasOne(t => t.CreatedByUser).WithMany().HasForeignKey(t => t.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.ParentTask).WithMany(t => t.SubTasks).HasForeignKey(t => t.ParentTaskId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(t => t.Assignments).WithOne(a => a.Task).HasForeignKey(a => a.TaskId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(t => t.StatusHistory).WithOne(h => h.Task).HasForeignKey(h => h.TaskId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(t => t.Comments).WithOne(c => c.Task).HasForeignKey(c => c.TaskId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(t => t.Attachments).WithOne(a => a.Task).HasForeignKey(a => a.TaskId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(t => t.TaskTags).WithOne(tt => tt.Task).HasForeignKey(tt => tt.TaskId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => new { t.ProjectId, t.Status });
            e.HasIndex(t => new { t.ProjectId, t.DueDate });
        });

        // ── TaskAssignment ────────────────────────────────────
        b.Entity<TaskAssignment>(e =>
        {
            e.ToTable("TaskAssignments");
            e.HasKey(a => a.AssignmentId);
            e.HasIndex(a => new { a.TaskId, a.AssignedToUserId }).IsUnique();
            e.HasOne(a => a.AssignedTo).WithMany(u => u.TaskAssignments).HasForeignKey(a => a.AssignedToUserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.AssignedBy).WithMany().HasForeignKey(a => a.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── TaskStatusHistory ─────────────────────────────────
        b.Entity<TaskStatusHistory>(e =>
        {
            e.ToTable("TaskStatusHistory");
            e.HasKey(h => h.HistoryId);
            e.Property(h => h.OldStatus).HasConversion<string>();
            e.Property(h => h.NewStatus).HasConversion<string>();
            e.Property(h => h.Note).HasMaxLength(500);
            e.HasOne(h => h.ChangedByUser).WithMany().HasForeignKey(h => h.ChangedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Tag ───────────────────────────────────────────────
        b.Entity<Tag>(e =>
        {
            e.ToTable("Tags");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("TagId").ValueGeneratedOnAdd();
            e.Property(t => t.Name).IsRequired().HasMaxLength(50);
            e.Property(t => t.Color).HasMaxLength(7).HasDefaultValue("#6366F1");
            e.HasOne(t => t.Project).WithMany(p => p.Tags).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        // ── TaskTag ───────────────────────────────────────────
        b.Entity<TaskTag>(e =>
        {
            e.ToTable("TaskTags");
            e.HasKey(tt => new { tt.TaskId, tt.TagId });
            e.HasOne(tt => tt.Tag).WithMany(t => t.TaskTags).HasForeignKey(tt => tt.TagId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── Comment ───────────────────────────────────────────
        b.Entity<Comment>(e =>
        {
            e.ToTable("Comments");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("CommentId").ValueGeneratedOnAdd();
            e.Property(c => c.Body).IsRequired().HasMaxLength(5000);
            e.HasOne(c => c.User).WithMany(u => u.Comments).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.ParentComment).WithMany(c => c.Replies).HasForeignKey(c => c.ParentCommentId).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Attachment ────────────────────────────────────────
        b.Entity<Attachment>(e =>
        {
            e.ToTable("Attachments");
            e.HasKey(a => a.Id);
            e.Property(a => a.Id).HasColumnName("AttachmentId").ValueGeneratedOnAdd();
            e.Property(a => a.FileName).IsRequired().HasMaxLength(260);
            e.Property(a => a.MimeType).IsRequired().HasMaxLength(100);
            e.Property(a => a.StorageUrl).IsRequired().HasMaxLength(1000);
            e.HasOne(a => a.Uploader).WithMany().HasForeignKey(a => a.UploadedBy).OnDelete(DeleteBehavior.Restrict);
        });

        // ── Notification ──────────────────────────────────────
        b.Entity<Notification>(e =>
        {
            e.ToTable("Notifications");
            e.HasKey(n => n.NotificationId);
            e.Property(n => n.Type).HasConversion<string>();
            e.Property(n => n.Title).IsRequired().HasMaxLength(200);
            e.Property(n => n.Message).IsRequired().HasMaxLength(500);
            e.Property(n => n.ActionUrl).HasMaxLength(500);
            e.HasOne(n => n.Recipient).WithMany(u => u.Notifications).HasForeignKey(n => n.RecipientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(n => n.Actor).WithMany().HasForeignKey(n => n.ActorId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(n => n.Task).WithMany().HasForeignKey(n => n.TaskId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAt });
        });

        // ── NotificationSetting ───────────────────────────────
        b.Entity<NotificationSetting>(e =>
        {
            e.ToTable("NotificationSettings");
            e.HasKey(s => s.SettingId);
            e.HasOne(s => s.User).WithOne(u => u.NotificationSettings).HasForeignKey<NotificationSetting>(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
