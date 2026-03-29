using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

public abstract class EntityWithIdentifier : Entity
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