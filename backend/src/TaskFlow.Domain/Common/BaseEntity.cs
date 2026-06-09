namespace TaskFlow.Domain.Common;

/// <summary>Base class for all domain entities — provides Id, CreatedAt, UpdatedAt</summary>
public abstract class BaseEntity
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
