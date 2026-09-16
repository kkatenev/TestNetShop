namespace ShopApi.Models;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

public record LoginRequest(string UserName, string Password);

public record RegisterRequest(string UserName, string Password);

public record LoginResponse(string AccessToken, string UserName, DateTimeOffset ExpiresAt);
