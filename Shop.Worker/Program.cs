using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shop.Contracts;
using Shop.Data;
using Shop.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ActivityLogContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMq:Host"] ?? "localhost";
        var username = builder.Configuration["RabbitMq:Username"] ?? "shop";
        var password = builder.Configuration["RabbitMq:Password"] ?? "shop";

        cfg.Host(host, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });

        cfg.ReceiveEndpoint(QueueNames.OrderCreated, e =>
        {
            e.ConfigureConsumer<OrderCreatedConsumer>(context);
        });
    });
});

var host = builder.Build();
host.Run();
