using HBP.Domain.Enums;

namespace HBP.Application.Common;

public static class Localized
{
    public static string? Pick(LanguageCode language, string? vi, string? ja) =>
        language == LanguageCode.Ja && !string.IsNullOrEmpty(ja) ? ja : vi;
}
