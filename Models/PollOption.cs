using System.ComponentModel.DataAnnotations.Schema;

public class PollOption : EntityWithIdentifier
{
    public string Name { get; set; }
    public string? Description { get; set; }

    [ForeignKey(nameof(Poll))]
    public int PollId { get; set; }
    public Poll? Poll { get; set; }

    public List<Vote> Votes { get; set; }
}