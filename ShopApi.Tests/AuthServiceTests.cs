using ShopApi.Models;
using ShopApi.Services;

namespace ShopApi.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task Register_ValidUser_ReturnsTokenAndSavesUser()
    {
        await using var db = TestHelpers.CreateDb();
        var auth = TestHelpers.CreateAuthService(db);

        var (response, error) = await auth.RegisterAsync(new RegisterRequest("alice", "secret1"));

        Assert.Null(error);
        Assert.NotNull(response);
        Assert.Equal("alice", response.UserName);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.True(response.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Single(db.Users);
        Assert.Equal("alice", db.Users.Single().UserName);
    }

    [Theory]
    [InlineData("ab", "secret1", "Логин должен быть не короче 3 символов")]
    [InlineData("alice", "123", "Пароль должен быть не короче 6 символов")]
    public async Task Register_InvalidInput_ReturnsError(string userName, string password, string expectedError)
    {
        await using var db = TestHelpers.CreateDb();
        var auth = TestHelpers.CreateAuthService(db);

        var (response, error) = await auth.RegisterAsync(new RegisterRequest(userName, password));

        Assert.Null(response);
        Assert.Equal(expectedError, error);
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Register_DuplicateUserName_ReturnsError()
    {
        await using var db = TestHelpers.CreateDb();
        var auth = TestHelpers.CreateAuthService(db);
        await auth.RegisterAsync(new RegisterRequest("alice", "secret1"));

        var (response, error) = await auth.RegisterAsync(new RegisterRequest("alice", "secret2"));

        Assert.Null(response);
        Assert.Equal("Такой логин уже занят", error);
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        await using var db = TestHelpers.CreateDb();
        var auth = TestHelpers.CreateAuthService(db);
        await auth.RegisterAsync(new RegisterRequest("bob", "password"));

        var response = await auth.LoginAsync(new LoginRequest("bob", "password"));

        Assert.NotNull(response);
        Assert.Equal("bob", response.UserName);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsNull()
    {
        await using var db = TestHelpers.CreateDb();
        var auth = TestHelpers.CreateAuthService(db);
        await auth.RegisterAsync(new RegisterRequest("bob", "password"));

        var response = await auth.LoginAsync(new LoginRequest("bob", "wrong-pass"));

        Assert.Null(response);
    }

    [Fact]
    public async Task Login_UnknownUser_ReturnsNull()
    {
        await using var db = TestHelpers.CreateDb();
        var auth = TestHelpers.CreateAuthService(db);

        var response = await auth.LoginAsync(new LoginRequest("nobody", "password"));

        Assert.Null(response);
    }
}
