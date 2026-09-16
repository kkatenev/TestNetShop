using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Shop.Contracts;
using ShopApi.Hubs;

namespace ShopApi.Consumers;

public class MailReceivedConsumer(IHubContext<MailHub> hub) : IConsumer<MailReceived>
{
    public Task Consume(ConsumeContext<MailReceived> context)
    {
        var mail = context.Message;
        return hub.Clients.Group(mail.UserName).SendAsync(
            "mailReceived",
            new
            {
                id = mail.Id,
                createdAt = mail.CreatedAt,
                taskId = mail.TaskId,
                message = mail.Message
            });
    }
}
