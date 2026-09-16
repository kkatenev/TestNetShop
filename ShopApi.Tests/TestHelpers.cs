using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShopApi.Data;
using ShopApi.Models;
using ShopApi.Services;

namespace ShopApi.Tests;

internal static class TestHelpers
{
    public static ShopDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ShopDbContext(options);
    }

    public static IConfiguration CreateJwtConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ShopApi-Test-Secret-Key-At-Least-32-Chars!",
                ["Jwt:Issuer"] = "ShopApi",
                ["Jwt:Audience"] = "ShopWeb"
            })
            .Build();

    public static AuthService CreateAuthService(ShopDbContext db) =>
        new(db, CreateJwtConfig(), new PasswordHasher<User>());

    public static Product SeedProduct(ShopDbContext db, string name = "Кофе", decimal price = 350m)
    {
        var product = new Product
        {
            Name = name,
            Description = $"{name} description",
            Price = price,
            ImageUrl = "/products/coffee.jpg"
        };
        db.Products.Add(product);
        db.SaveChanges();
        return product;
    }
}
