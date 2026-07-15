namespace NotificationService.Email.Contracts;

using NotificationService.Email.Models;

/// <summary>
/// Template + Model birleşiminden derlenmiş HTML üretir.
/// İçerisinde business logic bulunmaz, yalnızca render işlemi yapılır.
/// </summary>
public interface ITemplateRenderer
{
    Task<string> RenderAsync(string templateName, EmailTemplateModel model, CancellationToken cancellationToken = default);
}
