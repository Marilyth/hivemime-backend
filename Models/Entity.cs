using System.ComponentModel.DataAnnotations;

public abstract class EntityWithIdentifier : Entity
{
    [Key]
    public int Id { get; set; }
}

public abstract class Entity
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset DeletedAt { get; set; }

    [Timestamp]
    public uint RowVersion { get; set; }
}