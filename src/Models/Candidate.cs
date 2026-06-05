using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

[Index(nameof(NormalizedName), nameof(PollId), IsUnique = true)]
public class Candidate : EntityWithIdentifier
{
    [MaxLength(128)]
    public string NormalizedName { get; set; }
    [MaxLength(128)]
    public string Name { get; set; }

    [MaxLength(256)]
    public string? Description { get; set; }
    public List<string> MediaKeys { get; set; } = [];

    [ForeignKey(nameof(Poll))]
    public Guid PollId { get; set; }
    public Poll? Poll { get; set; }

    public List<CandidateVote> Votes { get; set; }
    public int Order { get; set; }
    public bool IsCustom { get; set; }
}