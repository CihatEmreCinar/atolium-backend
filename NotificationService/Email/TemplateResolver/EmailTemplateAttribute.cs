namespace NotificationService.Email.TemplateResolver;

/// <summary>
/// Bir email event sınıfını, kullanacağı template dosyasına bağlar.
/// Yeni bir email türü eklerken Consumer, Pipeline veya Resolver'a
/// dokunmaya gerek yoktur — sadece event sınıfına bu attribute eklenir.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EmailTemplateAttribute(string templateName) : Attribute
{
    public string TemplateName { get; } = templateName;
}
