namespace NotificationService.Email.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NotificationService.Email.Contracts;
using NotificationService.Email.Events;
using NotificationService.Email.Models;
using NotificationService.Infrastructure;

/// <summary>
/// RabbitMQ Message -> Template Resolver -> Model Builder -> Template Renderer
/// -> Email Provider zincirini yürüten tek orkestrasyon noktası. Her katman
/// birbirinden bağımsızdır; pipeline yalnızca sırayı ve hata/log akışını yönetir.
/// </summary>
public sealed class EmailPipeline : IEmailPipeline
{
    private readonly ITemplateResolver _templateResolver;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IEmailProvider _emailProvider;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailPipeline> _logger;

    public EmailPipeline(
        ITemplateResolver templateResolver,
        ITemplateRenderer templateRenderer,
        IEmailProvider emailProvider,
        IServiceProvider serviceProvider,
        ILogger<EmailPipeline> logger)
    {
        _templateResolver = templateResolver;
        _templateRenderer = templateRenderer;
        _emailProvider = emailProvider;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ExecuteAsync(EmailEventBase emailEvent, CancellationToken cancellationToken = default)
    {
        var eventType = emailEvent.GetType();
        EmailPipelineLog.MessageReceived(_logger, eventType.Name, emailEvent.ToEmail);

        string templateName;
        try
        {
            templateName = _templateResolver.Resolve(eventType);
            EmailPipelineLog.TemplateResolved(_logger, eventType.Name, templateName);
        }
        catch (Exception ex)
        {
            EmailPipelineLog.TemplateNotFound(_logger, ex, eventType.Name);
            throw new EmailPipelineException($"'{eventType.Name}' için template çözümlenemedi.", ex);
        }

        var builder = _serviceProvider.GetKeyedService<IEmailBuilder>(eventType)
            ?? throw new EmailPipelineException($"'{eventType.Name}' için kayıtlı bir IEmailBuilder bulunamadı.");

        EmailTemplateModel model;
        try
        {
            model = builder.Build(emailEvent);
            EmailPipelineLog.ModelBuilt(_logger, eventType.Name);
        }
        catch (Exception ex)
        {
            throw new EmailPipelineException($"'{eventType.Name}' için email modeli oluşturulamadı.", ex);
        }

        string html;
        try
        {
            html = await _templateRenderer.RenderAsync(templateName, model, cancellationToken);
            EmailPipelineLog.TemplateRendered(_logger, templateName);
        }
        catch (Exception ex)
        {
            EmailPipelineLog.RenderFailed(_logger, ex, templateName);
            throw new EmailPipelineException($"'{templateName}' render edilemedi.", ex);
        }

        var message = new EmailMessage
        {
            ToEmail = emailEvent.ToEmail,
            ToName = emailEvent.ToName,
            Subject = model.EmailTitle,
            HtmlBody = html,
            InlineImages = (model as IHasInlineImages)?.InlineImages,
        };

        try
        {
            await _emailProvider.SendAsync(message, cancellationToken);
            EmailPipelineLog.EmailSent(_logger, emailEvent.ToEmail, templateName);
        }
        catch (Exception ex)
        {
            EmailPipelineLog.EmailFailed(_logger, ex, emailEvent.ToEmail);
            EmailPipelineLog.SmtpError(_logger, ex, emailEvent.ToEmail);
            throw new EmailPipelineException("Email gönderilemedi.", ex);
        }
    }
}
