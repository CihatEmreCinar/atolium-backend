using CommunityPlatform.Application.Interfaces;
using CommunityPlatform.Infrastructure.Persistence;
using CommunityPlatform.Infrastructure.Services;
using CommunityPlatform.Infrastructure.Storage;
using MediaWorker.Consumers;
using MediaWorker.Services;
using Microsoft.EntityFrameworkCore;
using Minio;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// RabbitMQ — MediaReadyEvent'i yayınlamak için
builder.Services.AddSingleton<IConnection>(_ =>
{
    var rabbitConfig = builder.Configuration.GetSection("RabbitMQ");
    var factory = new ConnectionFactory
    {
        HostName = rabbitConfig["Host"] ?? "localhost",
        Port     = int.Parse(rabbitConfig["Port"] ?? "5672"),
        UserName = rabbitConfig["Username"] ?? "guest",
        Password = rabbitConfig["Password"] ?? "guest"
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});
builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

// MinIO — API ile aynı bucket
builder.Services.AddSingleton<IMinioClient>(_ =>
    new MinioClient()
        .WithEndpoint(builder.Configuration["Minio:Endpoint"] ?? "localhost:9000")
        .WithCredentials(
            builder.Configuration["Minio:AccessKey"] ?? "minioadmin",
            builder.Configuration["Minio:SecretKey"] ?? "minioadmin")
        .WithSSL(bool.Parse(builder.Configuration["Minio:UseSSL"] ?? "false"))
        .Build());
builder.Services.AddScoped<IMediaObjectStore, MinioMediaObjectStore>();

builder.Services.AddScoped<MediaProcessor>();
builder.Services.AddHostedService<MediaConsumer>();

var host = builder.Build();
host.Run();