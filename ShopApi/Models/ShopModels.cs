using System.Text.Json.Serialization;

namespace ShopApi.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}

public class Order
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "AwaitingPayment";
    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    [JsonIgnore]
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public record CreateOrderItemRequest(int ProductId, int Quantity);

public record CreateOrderRequest(List<CreateOrderItemRequest> Items);
