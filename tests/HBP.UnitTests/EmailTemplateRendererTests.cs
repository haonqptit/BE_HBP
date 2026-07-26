using HBP.Domain.Enums;
using HBP.Infrastructure.Email;

namespace HBP.UnitTests;

public sealed class EmailTemplateRendererTests
{
    private static readonly Dictionary<string, object?> CompleteModel = new()
    {
        ["site_name"] = "BB Homes",
        ["company_name"] = "BB Homes",
        ["related_entity_id"] = Guid.Parse("131638ef-2f81-4bea-ab7b-37024194d18f"),
        ["reference_code"] = "REQ-260726-ABC123",
        ["full_name"] = "Nguyễn An",
        ["email"] = "guest@example.com",
        ["phone_number"] = "0901234567",
        ["check_in_date"] = "17/08/2026",
        ["check_out_date"] = "19/08/2026",
        ["adults"] = 2,
        ["children"] = 1,
        ["number_of_rooms"] = 1,
        ["room_type_name"] = "Deluxe",
        ["customer_message"] = "Tôi cần nhận phòng sớm.",
        ["subject"] = "Thông tin lưu trú",
        ["message"] = "Xin tư vấn thêm giúp tôi."
    };

    [Fact]
    public async Task RendersJapaneseGuestTemplateInsideSharedLayout()
    {
        var renderer = new ScribanEmailTemplateRenderer();
        var result = await renderer.RenderAsync("BOOKING_GUEST_CONFIRMATION", LanguageCode.Ja,
            new Dictionary<string, object?> { ["site_name"] = "BB Homes", ["reference_code"] = "BK-260717-ABC123",
                ["full_name"] = "Taro", ["check_in_date"] = "17/07/2026", ["check_out_date"] = "19/07/2026",
                ["adults"] = 2, ["children"] = 0 }, default);

        Assert.Contains("BK-260717-ABC123", result.Subject);
        Assert.Contains("Taro", result.HtmlBody);
        Assert.Contains("ご予約リクエスト受付完了", result.HtmlBody);
        Assert.Contains("<!doctype html>", result.HtmlBody);
        Assert.Contains("https://bbhomesserviced.com/Logo.png", result.HtmlBody);
        Assert.Contains("©", result.HtmlBody);
    }

    [Fact]
    public async Task EscapesUntrustedTemplateValues()
    {
        var renderer = new ScribanEmailTemplateRenderer();
        var result = await renderer.RenderAsync("CONTACT_GUEST_CONFIRMATION", LanguageCode.Vi,
            new Dictionary<string, object?> { ["reference_code"] = "CT-001", ["full_name"] = "<script>alert(1)</script>",
                ["subject"] = "Hỗ trợ", ["email"] = "guest@example.com" }, default);

        Assert.DoesNotContain("<script>alert(1)</script>", result.HtmlBody);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", result.HtmlBody);
    }

    [Theory]
    [InlineData("BOOKING_ADMIN_NOTIFICATION", LanguageCode.Vi)]
    [InlineData("BOOKING_GUEST_CONFIRMATION", LanguageCode.Vi)]
    [InlineData("BOOKING_GUEST_CONFIRMATION", LanguageCode.Ja)]
    [InlineData("CONTACT_ADMIN_NOTIFICATION", LanguageCode.Vi)]
    [InlineData("CONTACT_GUEST_CONFIRMATION", LanguageCode.Vi)]
    [InlineData("CONTACT_GUEST_CONFIRMATION", LanguageCode.Ja)]
    public async Task RendersEveryEmailVariant(string emailType, LanguageCode language)
    {
        var result = await new ScribanEmailTemplateRenderer()
            .RenderAsync(emailType, language, CompleteModel, default);

        Assert.NotEmpty(result.Subject);
        Assert.Contains("REQ-260726-ABC123", result.HtmlBody);
        Assert.DoesNotContain("{{", result.HtmlBody);
        Assert.DoesNotContain("}}", result.HtmlBody);
    }
}
