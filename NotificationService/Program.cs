using NotificationService.Consumers;
using NotificationService.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddSingleton<EmailSenderService>();
        services.AddHostedService<EmailConsumer>();
    })
    .Build();

await host.RunAsync();
