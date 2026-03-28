public static class UserExtensions
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username
        };
    }

    public static UserDetailsDto ToDetailsDto(this User user)
    {
        return new UserDetailsDto
        {
            Username = user.Username,
            DateOfBirth = user.DateOfBirth,

            Settings = user.Settings.ToSettingsDto()
        };
    }

    public static UserSettingsDto ToSettingsDto(this UserSettings settings)
    {
        return new UserSettingsDto
        {
            Country = settings.Country,
            ShareDateOnVote = settings.ShareDateOnVote,
            ShareCountryOnVote = settings.ShareCountryOnVote,
            ShareAgeOnVote = settings.ShareAgeOnVote
        };
    }
}