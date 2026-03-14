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
            Id = user.Id,
            Username = user.Username,
            DateOfBirth = user.DateOfBirth,

            Settings = user.Settings.ToSettingsDto()
        };
    }

    public static UserSettingsDto ToSettingsDto(this UserSettings settings)
    {
        return new UserSettingsDto
        {
            ShareDateOfVote = settings.ShareDateOfVote,
            ShareCountryOfVote = settings.ShareCountryOfVote,
            ShareAgeOfVote = settings.ShareAgeOfVote
        };
    }
}