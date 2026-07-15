using NotificationService.Consumers;
using NotificationService.Infrastructure;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddEmailInfrastructure(ctx.Configuration);
    })
    .Build();

await host.RunAsync();
