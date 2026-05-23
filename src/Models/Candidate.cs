using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Candidate : EntityWithIdentifier
{
    [MaxLength(128)]
    public string Name { get; set; }
    [MaxLength(256)]
    public string? Description { get; set; }
    public List<string> MediaKeys { get; set; } = [];

    [ForeignKey(nameof(Poll))]
    public int PollId { get; set; }
    public Poll? Poll { get; set; }

    public List<CandidateVote> Votes { get; set; }
}