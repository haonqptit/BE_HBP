using HBP.Application.Abstractions;
using HBP.Domain.Enums;

namespace HBP.Api.Infrastructure;

public sealed class RequestLanguageAccessor : IRequestLanguageAccessor
{
    public LanguageCode Language { get; set; } = LanguageCode.Vi;
}
