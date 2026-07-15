namespace NotificationService.Infrastructure;

/// <summary>Pipeline'ın herhangi bir aşamasında (resolve/build/render/send) oluşan hataları merkezileştirir.</summary>
public sealed class EmailPipelineException : Exception
{
    public EmailPipelineException(string message) : base(message) { }
    public EmailPipelineException(string message, Exception innerException) : base(message, innerException) { }
}
