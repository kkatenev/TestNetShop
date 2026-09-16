using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shop.Contracts;
using ShopApi.Models;
using ShopApi.Services;

namespace ShopApi.Tests;

public class OrderServiceTests
{
    [Fact]
    public async Task Create_EmptyCart_ReturnsError()
    {
        await using var db = TestHelpers.CreateDb();
        var (service, _) = CreateService(db);

        var (order, error) = await service.CreateAsync("alice", new CreateOrderRequest([]));

        Assert.Null(order);
        Assert.Equal("Корзина пуста", error);
    }

    [Fact]
    public async Task Create_UnknownProduct_ReturnsError()
    {
        await using var db = TestHelpers.CreateDb();
        var (service, _) = CreateService(db);
        var request = new CreateOrderRequest([new CreateOrderItemRequest(999, 1)]);

        var (order, error) = await service.CreateAsync("alice", request);

        Assert.Null(order);
        Assert.Equal("Некоторые товары не найдены", error);
    }

    [Fact]
    public async Task Create_QuantityLessThanOne_ReturnsError()
    {
        await using var db = TestHelpers.CreateDb();
        var product = TestHelpers.SeedProduct(db);
        var (service, _) = CreateService(db);
        var request = new CreateOrderRequest([new CreateOrderItemRequest(product.Id, 0)]);

        var (order, error) = await service.CreateAsync("alice", request);

        Assert.Null(order);
        Assert.Equal("Количество должно быть больше 0", error);
    }

    [Fact]
    public async Task Create_ValidCart_SavesOrderAndSendsMessage()
    {
        await using var db = TestHelpers.CreateDb();
        var coffee = TestHelpers.SeedProduct(db, "Кофе", 350m);
        var tea = TestHelpers.SeedProduct(db, "Чай", 200m);
        var (service, sendEndpoint) = CreateService(db);
        var request = new CreateOrderRequest(
        [
            new CreateOrderItemRequest(coffee.Id, 2),
            new CreateOrderItemRequest(tea.Id, 1)
        ]);

        var (order, error) = await service.CreateAsync("alice", request);

        Assert.Null(error);
        Assert.NotNull(order);
        Assert.Equal("alice", order.UserName);
        Assert.Equal("AwaitingPayment", order.Status);
        Assert.Equal(900m, order.Total); // 350*2 + 200
        Assert.Equal(2, order.Items.Count);
        Assert.Contains(order.Items, i => i.ProductName == "Кофе" && i.Quantity == 2 && i.UnitPrice == 350m);
        Assert.Single(db.Orders);

        sendEndpoint.Verify(
            e => e.Send(It.Is<OrderCreated>(m =>
                m.OrderId == order.Id &&
                m.UserName == "alice" &&
                m.Total == 900m), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrders_ReturnsOnlyOrdersForUser()
    {
        await using var db = TestHelpers.CreateDb();
        var product = TestHelpers.SeedProduct(db);
        var (service, _) = CreateService(db);

        await service.CreateAsync("alice", new CreateOrderRequest([new CreateOrderItemRequest(product.Id, 1)]));
        await service.CreateAsync("bob", new CreateOrderRequest([new CreateOrderItemRequest(product.Id, 1)]));

        var aliceOrders = await service.GetOrdersAsync("alice");

        Assert.Single(aliceOrders);
        Assert.All(aliceOrders, o => Assert.Equal("alice", o.UserName));
    }

    static (OrderService Service, Mock<ISendEndpoint> Endpoint) CreateService(ShopApi.Data.ShopDbContext db)
    {
        var endpoint = new Mock<ISendEndpoint>();
        endpoint
            .Setup(e => e.Send(It.IsAny<OrderCreated>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var provider = new Mock<ISendEndpointProvider>();
        provider
            .Setup(p => p.GetSendEndpoint(It.IsAny<Uri>()))
            .ReturnsAsync(endpoint.Object);

        var service = new OrderService(db, provider.Object, NullLogger<OrderService>.Instance);
        return (service, endpoint);
    }
}
