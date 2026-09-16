using ShopApi.Services;

namespace ShopApi.Tests;

public class CatalogServiceTests
{
    [Fact]
    public async Task GetProducts_ReturnsOrderedById()
    {
        await using var db = TestHelpers.CreateDb();
        TestHelpers.SeedProduct(db, "Чай", 200m);
        TestHelpers.SeedProduct(db, "Кофе", 350m);
        var catalog = new CatalogService(db);

        var products = await catalog.GetProductsAsync();

        Assert.Equal(2, products.Count);
        Assert.Equal(["Чай", "Кофе"], products.Select(p => p.Name).ToArray());
        Assert.True(products[0].Id < products[1].Id);
    }

    [Fact]
    public async Task GetProducts_EmptyDatabase_ReturnsEmptyList()
    {
        await using var db = TestHelpers.CreateDb();
        var catalog = new CatalogService(db);

        var products = await catalog.GetProductsAsync();

        Assert.Empty(products);
    }
}
