public interface IUserService
{
    /// <summary>
    /// Creates a JWT token for the user if given, or a new anonymous user if null.
    /// </summary>
    /// <param name="user">The user to create a token for, or null to create a new anonymous user.</param>
    /// <returns>A LoginDto containing the JWT token and username.</returns>
    LoginDto Login(User? user);
}
