using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopApi.Models;

namespace ShopApi.Data;

public static class DbSeeder
{
    static readonly (string Name, string Description, decimal Price, string ImageUrl)[] Catalog =
    [
        ("Кофеварка Aero", "Компактная рожковая кофеварка для дома.", 12990, "/products/coffee.jpg"),
        ("Наушники Pulse", "Беспроводные наушники с шумоподавлением.", 8990, "/products/headphones.jpg"),
        ("Рюкзак Trail", "Городской рюкзак на 25 литров.", 5490, "/products/backpack.jpg"),
        ("Лампа Desk Soft", "Настольная лампа с тёплым светом.", 3190, "/products/lamp.jpg"),
        ("Клавиатура Mono", "Механическая клавиатура в компактном формате.", 7490, "/products/keyboard.jpg"),
        ("Бутылка Hydro", "Термобутылка 0.75 л из стали.", 1990, "/products/bottle.jpg"),
    ];

    public static async Task SeedAsync(ShopDbContext db, PasswordHasher<User> passwordHasher)
    {
        if (!await db.Users.AnyAsync())
        {
            var user = new User { UserName = "shop" };
            user.PasswordHash = passwordHasher.HashPassword(user, "shop123");
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        if (!await db.Products.AnyAsync())
        {
            db.Products.AddRange(Catalog.Select(item => new Product
            {
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                ImageUrl = item.ImageUrl
            }));
            await db.SaveChangesAsync();
            return;
        }

        // Обновляем старые ссылки на Unsplash локальными картинками
        var products = await db.Products.ToListAsync();
        var changed = false;
        foreach (var product in products)
        {
            var match = Catalog.FirstOrDefault(item => item.Name == product.Name);
            if (match.Name is null)
            {
                continue;
            }

            if (product.ImageUrl != match.ImageUrl)
            {
                product.ImageUrl = match.ImageUrl;
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }
}
