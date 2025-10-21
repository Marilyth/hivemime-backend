using System.ComponentModel.DataAnnotations.Schema;

public class Category : EntityWithIdentifier
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public int Color { get; set; }

    [ForeignKey(nameof(Poll))]
    public int PollId { get; set; }
    public Poll? Poll { get; set; }
}
