namespace NotificationService.Email.Rendering;

using System.Collections.Concurrent;
using HandlebarsDotNet;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NotificationService.Email.Contracts;
using NotificationService.Email.Models;
using NotificationService.Infrastructure;


/// <summary>
/// Renderer yalnızca Template + Model birleşimini yapar. İçerisinde business
/// logic bulunmaz. Derlenmiş template'ler bellekte cache'lenir; diskten
/// tekrar tekrar okunmaz/derlenmez.
/// </summary>
public sealed class HandlebarsTemplateRenderer : ITemplateRenderer
{
    private readonly string _templatesRoot;
    private readonly IHandlebars _handlebars;
    private readonly ConcurrentDictionary<string, HandlebarsTemplate<object, object>> _compiledCache = new();

    public HandlebarsTemplateRenderer(IOptions<EmailTemplateOptions> options, IHostEnvironment environment)
    {
        var configuredPath = options.Value.TemplatesDirectory;
        _templatesRoot = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);

        _handlebars = Handlebars.Create();
    }

    public Task<string> RenderAsync(string templateName, EmailTemplateModel model, CancellationToken cancellationToken = default)
    {
        var compiled = _compiledCache.GetOrAdd(templateName, LoadAndCompile);
        var html = compiled(model);
        return Task.FromResult(html);
    }

    private HandlebarsTemplate<object, object> LoadAndCompile(string templateName)
    {
        var path = Path.Combine(_templatesRoot, templateName);
        if (!File.Exists(path))
        {
            throw new EmailPipelineException($"Template dosyası bulunamadı: {templateName} ({path})");
        }

        var source = File.ReadAllText(path);
        return _handlebars.Compile(source);
    }
}
