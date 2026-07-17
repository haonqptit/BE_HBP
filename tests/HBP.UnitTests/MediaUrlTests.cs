using HBP.Application.Common;

namespace HBP.UnitTests;

public sealed class MediaUrlTests
{
    [Theory]
    [InlineData("https://api.test/media/2026/07/id/original.webp", "medium", "https://api.test/media/2026/07/id/medium.webp")]
    [InlineData("/media/id/original.webp", "thumbnail", "/media/id/thumbnail.webp")]
    public void Variant_ReplacesFileName(string original, string variant, string expected) =>
        Assert.Equal(expected, MediaUrl.Variant(original, variant));
}
