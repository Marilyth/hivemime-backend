using System.ComponentModel.DataAnnotations.Schema;

public abstract class Like : Entity
{
    [ForeignKey(nameof(User))]
    public int UserId { get; set; }
    public User? User { get; set; }

    public bool IsDislike { get; set; }
}