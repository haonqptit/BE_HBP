using System.Reflection;
using HBP.Application.Common;
using HBP.Application.Email;
using HBP.Domain.Enums;
using Scriban;
using Scriban.Runtime;

namespace HBP.Infrastructure.Email;

public sealed class ScribanEmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly Assembly Assembly = typeof(ScribanEmailTemplateRenderer).Assembly;

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
        var script = new ScriptObject();
        foreach (var pair in model) script[pair.Key] = pair.Value;
        var context = new TemplateContext(); context.PushGlobal(script);
        return new RenderedEmail(await Template.Parse(subject).RenderAsync(context), await Template.Parse(body).RenderAsync(context));
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
}
