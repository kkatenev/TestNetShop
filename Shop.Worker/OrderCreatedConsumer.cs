using MassTransit;
using Shop.Contracts;
using Shop.Data;

namespace Shop.Worker;

public class OrderCreatedConsumer(
    ActivityLogContext db,
    ISendEndpointProvider sendEndpoints,
    ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreated>
{
    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Получил заказ {OrderId} на {Total}. Имитирую письмо «оплатите»...",
            message.OrderId,
            message.Total);

        await Task.Delay(TimeSpan.FromSeconds(2));

        var text =
            $"На почте одно новое сообщение: Оплатите заказ #{message.OrderId} на сумму {message.Total:0} ₽";

        var log = new ActivityLog
        {
            TaskId = message.OrderId,
            CreatedAt = DateTimeOffset.UtcNow,
            Message = text
        };
        db.ActivityLogs.Add(log);
        await db.SaveChangesAsync();

        var endpoint = await sendEndpoints.GetSendEndpoint(new Uri($"queue:{QueueNames.MailReceived}"));
        await endpoint.Send(new MailReceived(
            log.Id,
            log.CreatedAt,
            log.TaskId,
            log.Message,
            message.UserName));

        logger.LogInformation("Письмо по заказу {OrderId} отправлено", message.OrderId);
    }
}
