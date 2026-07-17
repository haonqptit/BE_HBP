using HBP.Domain.Enums;
using HBP.Infrastructure.Email;

namespace HBP.UnitTests;

public sealed class EmailTemplateRendererTests
{
    [Fact]
    public async Task RendersJapaneseGuestTemplate()
    {
        var renderer = new ScribanEmailTemplateRenderer();
        var result = await renderer.RenderAsync("BOOKING_GUEST_CONFIRMATION", LanguageCode.Ja,
            new Dictionary<string, object?> { ["site_name"] = "HBP", ["reference_code"] = "BK-260717-ABC123", ["full_name"] = "Taro" }, default);
        Assert.Contains("BK-260717-ABC123", result.Subject);
        Assert.Contains("Taro", result.HtmlBody);
        Assert.Contains("受け付けました", result.HtmlBody);
    }
}
