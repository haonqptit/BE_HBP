using HBP.Application.Abstractions;
using HBP.Domain.Enums;

namespace HBP.Api.Infrastructure;

public sealed class LanguageResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IRequestLanguageAccessor accessor)
    {
        var requested = context.Request.Query["lang"].FirstOrDefault()
            ?? context.Request.GetTypedHeaders().AcceptLanguage?.FirstOrDefault()?.Value.Value;
        accessor.Language = requested?.StartsWith("ja", StringComparison.OrdinalIgnoreCase) == true
            ? LanguageCode.Ja
            : LanguageCode.Vi;
        await next(context);
    }
}
