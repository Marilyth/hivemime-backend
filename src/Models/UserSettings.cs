using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

[Owned]
public class UserSettings
{
    // Automatically deduced demographic data.
    [MaxLength(2)]
    public string? Country { get; set; }
    
    // Demographic data is optional. The user can opt out of sharing them.
    public bool ShareDateOnVote { get; set; } = true;
    public bool ShareCountryOnVote { get; set; } = true;
    public bool ShareAgeOnVote { get; set; } = true;
    public bool ProtectVoteOnFilter { get; set; } = false;
}