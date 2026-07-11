using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

public interface IHasIdentifier
{
    Guid Id { get; set; }
}

public class Entity : IHasIdentifier
{
    [Key]
    public Guid Id { get; set; }
}

[Index(nameof(CreatedAt))]
public abstract class RootEntity : Entity, IHasIdentifier
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; set; }
}