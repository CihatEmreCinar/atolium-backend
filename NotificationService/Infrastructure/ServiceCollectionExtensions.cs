namespace NotificationService.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Consumers;
using NotificationService.Email.Builders;
using NotificationService.Email.Contracts;
using NotificationService.Email.Events;
using NotificationService.Email.Providers;
using NotificationService.Email.Rendering;
using NotificationService.Email.Services;
using NotificationService.Email.TemplateResolver;

/// <summary>
/// Notification Worker Email Infrastructure'ın tüm bileşenlerini tek noktadan
/// kaydeder. Yeni bir email türü eklerken:
///   1) Events/ altına [EmailTemplate(...)] ile işaretli event sınıfı ekle
///   2) Builders/ altına EmailBuilderBase&lt;TEvent&gt; türeten builder ekle
///   3) RegisterBuilders içine tek satır AddKeyedScoped ekle
/// Consumer, Pipeline ve Provider'a KESİNLİKLE dokunulmaz.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmailInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EmailBrandOptions>(configuration.GetSection(EmailBrandOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<EmailTemplateOptions>(configuration.GetSection(EmailTemplateOptions.SectionName));

        services.AddSingleton<ITemplateResolver, DefaultTemplateResolver>();
        services.AddSingleton<ITemplateRenderer, HandlebarsTemplateRenderer>();
        services.AddSingleton<EmailEventTypeRegistry>();
        services.AddScoped<IEmailPipeline, EmailPipeline>();

        var useConsoleProvider = configuration.GetValue("Email:UseConsoleProvider", false);
        if (useConsoleProvider)
        {
            services.AddSingleton<IEmailProvider, NullEmailProvider>();
        }
        else
        {
            services.AddSingleton<IEmailProvider, SmtpEmailProvider>();
        }

        RegisterBuilders(services);

        services.AddHostedService<EmailEventConsumer>();

        return services;
    }

    private static void RegisterBuilders(IServiceCollection services)
    {
        services.AddKeyedScoped<IEmailBuilder, WelcomeEmailBuilder>(typeof(WelcomeEvent));
        services.AddKeyedScoped<IEmailBuilder, VerifyEmailBuilder>(typeof(VerifyEmailEvent));
        services.AddKeyedScoped<IEmailBuilder, MagicLinkEmailBuilder>(typeof(MagicLinkEvent));
        services.AddKeyedScoped<IEmailBuilder, PasswordResetEmailBuilder>(typeof(PasswordResetEvent));
        services.AddKeyedScoped<IEmailBuilder, PasswordChangedEmailBuilder>(typeof(PasswordChangedEvent));
        services.AddKeyedScoped<IEmailBuilder, SecurityAlertEmailBuilder>(typeof(SecurityAlertEvent));
        services.AddKeyedScoped<IEmailBuilder, AccountDeletedEmailBuilder>(typeof(AccountDeletedEvent));

        services.AddKeyedScoped<IEmailBuilder, WorkshopRegistrationEmailBuilder>(typeof(WorkshopRegistrationEvent));
        services.AddKeyedScoped<IEmailBuilder, WorkshopApprovedEmailBuilder>(typeof(WorkshopApprovedEvent));
        services.AddKeyedScoped<IEmailBuilder, WorkshopReminderEmailBuilder>(typeof(WorkshopReminderEvent));
        services.AddKeyedScoped<IEmailBuilder, WorkshopCancelledEmailBuilder>(typeof(WorkshopCancelledEvent));
        services.AddKeyedScoped<IEmailBuilder, WaitlistEmailBuilder>(typeof(WaitlistEvent));

        services.AddKeyedScoped<IEmailBuilder, CommunityInvitationEmailBuilder>(typeof(CommunityInvitationEvent));
        services.AddKeyedScoped<IEmailBuilder, OrganizerMessageEmailBuilder>(typeof(OrganizerMessageEvent));
        services.AddKeyedScoped<IEmailBuilder, NewsletterEmailBuilder>(typeof(NewsletterEvent));
        services.AddKeyedScoped<IEmailBuilder, GenericNotificationEmailBuilder>(typeof(GenericNotificationEvent));

        services.AddKeyedScoped<IEmailBuilder, TicketEmailBuilder>(typeof(TicketEvent));
        services.AddKeyedScoped<IEmailBuilder, InvoiceEmailBuilder>(typeof(InvoiceEvent));
        services.AddKeyedScoped<IEmailBuilder, PaymentReceiptEmailBuilder>(typeof(PaymentReceiptEvent));

        services.AddKeyedScoped<IEmailBuilder, CertificateEmailBuilder>(typeof(CertificateEvent));
        services.AddKeyedScoped<IEmailBuilder, AchievementEmailBuilder>(typeof(AchievementEvent));
    }
}
