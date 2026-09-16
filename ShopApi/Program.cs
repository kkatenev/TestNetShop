using System.Security.Claims;
using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shop.Contracts;
using ShopApi.Consumers;
using ShopApi.Data;
using ShopApi.Hubs;
using ShopApi.Models;
using ShopApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSignalR();
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});
builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddSingleton<PasswordHasher<User>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<OrderService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };

        // SignalR передаёт JWT через query: ?access_token=...
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MailReceivedConsumer>();

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

        cfg.ReceiveEndpoint(QueueNames.MailReceived, e =>
        {
            e.ConfigureConsumer<MailReceivedConsumer>(context);
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
if (app.Configuration.GetValue("HttpsRedirection", true))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<MailHub>("/hubs/mail");

app.MapPost("/auth/login", async (LoginRequest request, AuthService auth) =>
{
    if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest("UserName and Password are required");
    }

    var result = await auth.LoginAsync(request);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
});

app.MapPost("/auth/register", async (RegisterRequest request, AuthService auth) =>
{
    if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest("UserName and Password are required");
    }

    var (result, error) = await auth.RegisterAsync(request);
    if (error is not null || result is null)
    {
        return Results.BadRequest(error ?? "Не удалось зарегистрироваться");
    }

    return Results.Created("/auth/login", result);
});

var api = app.MapGroup("").RequireAuthorization();

api.MapGet("/activity-logs", async (ClaimsPrincipal user, ShopDbContext db) =>
{
    var userName = user.Identity?.Name;
    if (string.IsNullOrWhiteSpace(userName))
    {
        return Results.Unauthorized();
    }

    var logs = await db.ActivityLogs
        .Where(log =>
            log.Message.StartsWith("На почте одно новое сообщение:") &&
            log.TaskId != null &&
            db.Orders.Any(o => o.Id == log.TaskId && o.UserName == userName))
        .OrderByDescending(log => log.Id)
        .Take(40)
        .ToListAsync();

    return Results.Ok(logs);
});

api.MapGet("/products", async (CatalogService catalog) => await catalog.GetProductsAsync());

api.MapGet("/orders", async (ClaimsPrincipal user, OrderService orders) =>
{
    var userName = user.Identity?.Name;
    if (string.IsNullOrWhiteSpace(userName))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(await orders.GetOrdersAsync(userName));
});

api.MapPost("/orders", async (ClaimsPrincipal user, CreateOrderRequest request, OrderService orders) =>
{
    var userName = user.Identity?.Name;
    if (string.IsNullOrWhiteSpace(userName))
    {
        return Results.Unauthorized();
    }

    var (order, error) = await orders.CreateAsync(userName, request);
    if (error is not null || order is null)
    {
        return Results.BadRequest(error ?? "Не удалось создать заказ");
    }

    return Results.Created($"/orders/{order.Id}", order);
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, passwordHasher);
}

app.Run();
