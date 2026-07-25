using HBP.Application.Admin;
using HBP.Domain.Enums;

namespace HBP.UnitTests;

public class AdminValidatorTests
{
    private static SaveRoomTypeRequest Room(PriceDisplayMode mode, decimal? vnd = null, decimal? usd = null,
        string slug = "phong-deluxe") =>
        new("DLX", slug, "Phòng Deluxe", null, null, null, null, null, vnd, usd, mode, 2,
            null, null, null, null, 0, true, null, null, null, null);

    [Fact]
    public void ShowPriceRequiresAtLeastOneCurrency()
    {
        var result = new SaveRoomTypeRequestValidator().Validate(Room(PriceDisplayMode.SHOW_PRICE));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(SaveRoomTypeRequest.PriceVnd));
    }

    [Fact]
    public void ShowPriceAcceptsVndOnly() =>
        Assert.True(new SaveRoomTypeRequestValidator()
            .Validate(Room(PriceDisplayMode.SHOW_PRICE, vnd: 1_000_000m)).IsValid);

    [Fact]
    public void ShowPriceAcceptsUsdOnly() =>
        Assert.True(new SaveRoomTypeRequestValidator()
            .Validate(Room(PriceDisplayMode.SHOW_PRICE, usd: 40m)).IsValid);

    [Fact]
    public void ContactModeNeedsNoPrice()
    {
        Assert.True(new SaveRoomTypeRequestValidator().Validate(Room(PriceDisplayMode.CONTACT)).IsValid);
    }

    [Theory]
    [InlineData("Phong-Deluxe")]
    [InlineData("phong deluxe")]
    [InlineData("phong--deluxe")]
    [InlineData("-phong")]
    public void SlugRejectsNonKebabCase(string slug)
    {
        var result = new SaveRoomTypeRequestValidator().Validate(Room(PriceDisplayMode.CONTACT, slug: slug));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(SaveRoomTypeRequest.Slug));
    }

    [Fact]
    public void GalleryItemRequiresCategoryAndMedia()
    {
        var result = new SaveGalleryItemRequestValidator()
            .Validate(new SaveGalleryItemRequest(Guid.Empty, Guid.Empty, null, null, 0, true));
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void ReplaceLinksRejectsNegativeDisplayOrder()
    {
        var result = new ReplaceLinksRequestValidator()
            .Validate(new ReplaceLinksRequest([new OrderedLinkRequest(Guid.NewGuid(), -1)]));
        Assert.False(result.IsValid);
    }
}
