using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

public interface IHasIdentifier
{
    int Id { get; set; }
}

public abstract class EntityWithIdentifier : Entity, IHasIdentifier
{
    [Key]
    public int Id { get; set; }
}

[Index(nameof(CreatedAt))]
public abstract class Entity
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }

    [Timestamp]
    public uint RowVersion { get; set; }
}