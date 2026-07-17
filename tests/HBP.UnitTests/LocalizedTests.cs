using HBP.Application.Common;
using HBP.Domain.Enums;

namespace HBP.UnitTests;

public sealed class LocalizedTests
{
    [Fact] public void Japanese_UsesJapanese() => Assert.Equal("ja", Localized.Pick(LanguageCode.Ja, "vi", "ja"));
    [Fact] public void Japanese_FallsBackForNull() => Assert.Equal("vi", Localized.Pick(LanguageCode.Ja, "vi", null));
    [Fact] public void Japanese_FallsBackForEmpty() => Assert.Equal("vi", Localized.Pick(LanguageCode.Ja, "vi", ""));
    [Fact] public void Vietnamese_AlwaysUsesVietnamese() => Assert.Equal("vi", Localized.Pick(LanguageCode.Vi, "vi", "ja"));
}
