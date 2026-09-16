using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShopApi.Data;
using ShopApi.Models;

namespace ShopApi.Services;

public class AuthService(
    ShopDbContext db,
    IConfiguration configuration,
    PasswordHasher<User> passwordHasher)
{
    public async Task<(LoginResponse? Response, string? Error)> RegisterAsync(RegisterRequest request)
    {
        var userName = request.UserName.Trim();
        if (userName.Length < 3)
        {
            return (null, "Логин должен быть не короче 3 символов");
        }

        if (request.Password.Length < 6)
        {
            return (null, "Пароль должен быть не короче 6 символов");
        }

        if (await db.Users.AnyAsync(u => u.UserName == userName))
        {
            return (null, "Такой логин уже занят");
        }

        var user = new User { UserName = userName };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);
        return (new LoginResponse(CreateToken(user, expiresAt), user.UserName, expiresAt), null);
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var userName = request.UserName.Trim();
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == userName);
        if (user is null)
        {
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);
        var token = CreateToken(user, expiresAt);

        return new LoginResponse(token, user.UserName, expiresAt);
    }

    string CreateToken(User user, DateTimeOffset expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim(ClaimTypes.Name, user.UserName)
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
