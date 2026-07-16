using HBP.Domain.Enums;

namespace HBP.Application.Abstractions;

public interface IRequestLanguageAccessor
{
    LanguageCode Language { get; set; }
}
