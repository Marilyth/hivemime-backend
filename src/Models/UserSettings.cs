public class UserSettings : EntityWithIdentifier
{
    // Demographic data is optional. The user can opt out of sharing them.
    public bool ShowGenderOnVote { get; set; } = true;
    public bool ShowCountryOnVote { get; set; } = true;
    public bool ShowAgeOnVote { get; set; } = true;
}