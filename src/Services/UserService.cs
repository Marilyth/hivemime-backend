using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public class UserService(HiveMimeContext context, IConfiguration configuration) : IUserService
{
    public LoginDto Login(string username)
    {
        // TODO: Add security measures / actual login.
        User user = context.Users.FirstOrDefault(u => u.Username == username) ?? CreateUser(username);

        Claim[] claims = [
            new Claim("UserId", user.Id.ToString())
        ];

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(999),
            signingCredentials: creds
        );

        string tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginDto { Token = tokenString, Username = user.Username };
    }

    public UserDetailsDto GetUserDetails(int userId)
    {
        var user = context.Users.AsNoTracking().Where(u => u.Id == userId).Include(u => u.Settings).First();
        return user.ToDetailsDto();
    }

    public User CreateUser(string username)
    {
        // TODO: Add support for registration using password / emails / OAuth, etc.
        var user = new User()
        {
            Username = username,
            Settings = new()
        };

        context.Users.Add(user);
        context.SaveChanges();

        return user;
    }
}