namespace NotificationService.Infrastructure;

using Microsoft.Extensions.Logging;

/// <summary>
/// Pipeline'ın her aşaması için source-generated, merkezi structured logging.
/// Error Handling spesifikasyonundaki tüm aşamalar (Message Received, Template
/// Resolved, Model Built, Template Rendered, Email Sent, Email Failed,
/// Render Failed, Template Not Found, SMTP Error) burada tanımlıdır.
/// </summary>
public static partial class EmailPipelineLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Email mesajı alındı: {EventType} -> {Recipient}")]
    public static partial void MessageReceived(ILogger logger, string eventType, string recipient);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "Template çözümlendi: {EventType} -> {TemplateName}")]
    public static partial void TemplateResolved(ILogger logger, string eventType, string templateName);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Debug, Message = "Email modeli oluşturuldu: {EventType}")]
    public static partial void ModelBuilt(ILogger logger, string eventType);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Debug, Message = "Template render edildi: {TemplateName}")]
    public static partial void TemplateRendered(ILogger logger, string templateName);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information, Message = "Email gönderildi: {Recipient} ({TemplateName})")]
    public static partial void EmailSent(ILogger logger, string recipient, string templateName);

    [LoggerMessage(EventId = 1100, Level = LogLevel.Error, Message = "Email gönderilemedi: {Recipient}")]
    public static partial void EmailFailed(ILogger logger, Exception exception, string recipient);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Error, Message = "Template render hatası: {TemplateName}")]
    public static partial void RenderFailed(ILogger logger, Exception exception, string templateName);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Error, Message = "Template bulunamadı: {EventType}")]
    public static partial void TemplateNotFound(ILogger logger, Exception exception, string eventType);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Error, Message = "SMTP hatası: {Recipient}")]
    public static partial void SmtpError(ILogger logger, Exception exception, string recipient);
}
