using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shop.Contracts;
using ShopApi.Data;
using ShopApi.Models;

namespace ShopApi.Services;

public class CatalogService(ShopDbContext db)
{
    public Task<List<Product>> GetProductsAsync() =>
        db.Products.OrderBy(p => p.Id).ToListAsync();
}

public class OrderService(
    ShopDbContext db,
    ISendEndpointProvider sendEndpoints,
    ILogger<OrderService> logger)
{
    public async Task<List<Order>> GetOrdersAsync(string userName)
    {
        return await db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserName == userName)
            .OrderByDescending(o => o.Id)
            .ToListAsync();
    }

    public async Task<(Order? Order, string? Error)> CreateAsync(string userName, CreateOrderRequest request)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            return (null, "Корзина пуста");
        }

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Count)
        {
            return (null, "Некоторые товары не найдены");
        }

        var order = new Order
        {
            UserName = userName,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = "AwaitingPayment"
        };

        foreach (var line in request.Items)
        {
            if (line.Quantity < 1)
            {
                return (null, "Количество должно быть больше 0");
            }

            var product = products[line.ProductId];
            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = line.Quantity
            });
        }

        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var endpoint = await sendEndpoints.GetSendEndpoint(new Uri($"queue:{QueueNames.OrderCreated}"));
        await endpoint.Send(new OrderCreated(order.Id, order.UserName, order.Total));
        logger.LogInformation("Отправил заказ {OrderId} в очередь {Queue}", order.Id, QueueNames.OrderCreated);

        return (order, null);
    }
}
