namespace Shop.Contracts;

public static class QueueNames
{
    public const string OrderCreated = "order-created";
    public const string MailReceived = "mail-received";
}

public record OrderCreated(int OrderId, string UserName, decimal Total);

public record MailReceived(
    int Id,
    DateTimeOffset CreatedAt,
    int? TaskId,
    string Message,
    string UserName);
