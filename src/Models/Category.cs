using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Category : EntityWithIdentifier
{
    [MaxLength(64)]
    public string Name { get; set; }
    [MaxLength(256)]
    public string? Description { get; set; }
    public int Color { get; set; }

    [ForeignKey(nameof(Poll))]
    public int PollId { get; set; }
    public Poll? Poll { get; set; }

    public int Value { get; set; }
}
