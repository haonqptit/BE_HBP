using System.Reflection;
using HBP.Application.Common;
using HBP.Application.Email;
using HBP.Domain.Enums;
using Microsoft.Extensions.Options;
using Scriban;
using Scriban.Runtime;

namespace HBP.Infrastructure.Email;

public sealed class ScribanEmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Assembly Assembly = typeof(ScribanEmailTemplateRenderer).Assembly;
    private readonly EmailBrandOptions _brand;

    public ScribanEmailTemplateRenderer(IOptions<EmailBrandOptions>? brand = null)
    {
        _brand = brand?.Value ?? new EmailBrandOptions();
    }

    public async Task<RenderedEmail> RenderAsync(string emailType, LanguageCode language,
        IReadOnlyDictionary<string, object?> model, CancellationToken cancellationToken)
    {
        var lang = language == LanguageCode.Ja ? "ja" : "vi";
        var subject = await LoadAsync(emailType, lang, "subject", cancellationToken)
            ?? await LoadAsync(emailType, "vi", "subject", cancellationToken)
            ?? throw new NotFoundException($"Email subject template not found for {emailType}.");
        var body = await LoadAsync(emailType, lang, "body", cancellationToken)
            ?? await LoadAsync(emailType, "vi", "body", cancellationToken)
            ?? throw new NotFoundException($"Email body template not found for {emailType}.");
        var layout = await LoadSharedAsync("layout", cancellationToken)
            ?? throw new NotFoundException("Shared email layout not found.");
        var script = new ScriptObject();
        script["website_url"] = _brand.WebsiteUrl.TrimEnd('/');
        script["logo_url"] = _brand.LogoUrl;
        script["company_name"] = _brand.CompanyName;
        script["company_address"] = _brand.Address;
        script["company_phone"] = _brand.Phone;
        script["company_email"] = _brand.Email;
        script["facebook_url"] = _brand.FacebookUrl;
        script["instagram_url"] = _brand.InstagramUrl;
        script["current_year"] = DateTimeOffset.UtcNow.Year;
        script["language_path"] = lang;
        foreach (var pair in model) script[pair.Key] = pair.Value;
        var renderedSubject = await RenderAsync(subject, script);
        script["email_subject"] = renderedSubject;
        script["preheader"] = renderedSubject;
        script["email_content"] = await RenderAsync(body, script);
        return new RenderedEmail(renderedSubject, await RenderAsync(layout, script));
    }

    private static async Task<string> RenderAsync(string source, ScriptObject script)
    {
        var template = Template.Parse(source);
        if (template.HasErrors)
            throw new InvalidOperationException(string.Join("; ", template.Messages.Select(x => x.Message)));
        var context = new TemplateContext();
        context.PushGlobal(script);
        return await template.RenderAsync(context);
    }

    private static async Task<string?> LoadAsync(string type, string lang, string part, CancellationToken cancellationToken)
    {
        var suffix = $"Email.Templates.{type}.{lang}.{part}.sbn";
        var name = Assembly.GetManifestResourceNames().SingleOrDefault(x => x.EndsWith(suffix, StringComparison.Ordinal));
        if (name is null) return null;
        await using var stream = Assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static async Task<string?> LoadSharedAsync(string part, CancellationToken cancellationToken)
    {
        var suffix = $"Email.Templates.Shared.{part}.sbn";
        var name = Assembly.GetManifestResourceNames().SingleOrDefault(x => x.EndsWith(suffix, StringComparison.Ordinal));
        if (name is null) return null;
        await using var stream = Assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
