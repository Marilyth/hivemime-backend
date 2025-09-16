using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[PrimaryKey(nameof(UserId), nameof(PollOptionId))]
public class Vote : Entity
{
    [ForeignKey(nameof(User))]
    public int UserId { get; set; }
    public User? User { get; set; }

    [ForeignKey(nameof(PollOption))]
    public int PollOptionId { get; set; }
    public PollOption? PollOption { get; set; }

    public int Value { get; set; }
}
