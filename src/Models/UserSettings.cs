public class UserSettings : EntityWithIdentifier
{
    // Demographic data is optional. The user can opt out of sharing them.
    public bool ShareDateOfVote { get; set; } = true;
    public bool ShareCountryOfVote { get; set; } = true;
    public bool ShareAgeOfVote { get; set; } = true;
}