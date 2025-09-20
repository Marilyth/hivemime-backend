using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public class UserService(HiveMimeContext context, IConfiguration configuration) : IUserService
{
    public LoginDto Login(User? user)
    {
        // For now, we use anonymous users.
        // If the user doesn't already have a token, create a new user.
        if (user is null)
        {
            user = new()
            {
                Username = "Anonymous"
            };

            context.Users.Add(user);
            context.SaveChanges();
        }

        Claim[] claims = [
            new Claim("UserId", user.Id.ToString())
        ];

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        string tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginDto { Token = tokenString, Username = user.Username };
    }
}