using HBP.Application.Abstractions;
using HBP.Application.Common;
using HBP.Application.Public;
using HBP.Domain.Enums;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Public;

public sealed class PublicRoomTypeQueryService(HbpDbContext db, IRequestLanguageAccessor languageAccessor)
    : IPublicRoomTypeQueryService
{
    public async Task<IReadOnlyList<RoomTypeListItemResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var lang = languageAccessor.Language;
        var rows = await db.RoomTypes.AsNoTracking().Include(x => x.FeaturedMedia)
            .Where(x => x.IsVisible).OrderBy(x => x.DisplayOrder).ThenBy(x => x.NameVi).ToListAsync(cancellationToken);
        return rows.Select(x => new RoomTypeListItemResponse(x.Id, x.Slug,
            Localized.Pick(lang, x.NameVi, x.NameJa)!, Localized.Pick(lang, x.ShortDescriptionVi, x.ShortDescriptionJa),
            x.Capacity, x.AreaSquareMeters, Localized.Pick(lang, x.BedDescriptionVi, x.BedDescriptionJa), x.PriceDisplayMode,
            x.PriceDisplayMode == PriceDisplayMode.SHOW_PRICE ? x.PriceVnd : null,
            x.PriceDisplayMode == PriceDisplayMode.SHOW_PRICE ? x.PriceUsd : null,
            PublicMapping.Image(x.FeaturedMedia, lang), x.DisplayOrder)).ToList();
    }

    public async Task<RoomTypeDetailResponse> GetAsync(string slug, CancellationToken cancellationToken)
    {
        var lang = languageAccessor.Language;
        var x = await db.RoomTypes.AsNoTracking().AsSplitQuery().Include(r => r.FeaturedMedia)
            .Include(r => r.RoomTypeAmenities).ThenInclude(a => a.Amenity)
            .Include(r => r.RoomTypeMedia).ThenInclude(m => m.MediaFile)
            .SingleOrDefaultAsync(r => r.Slug == slug && r.IsVisible, cancellationToken)
            ?? throw new NotFoundException("Room type not found.");
        var amenities = x.RoomTypeAmenities.Where(a => a.Amenity.IsVisible)
            .OrderBy(a => a.DisplayOrder ?? 0).ThenBy(a => a.Amenity.DisplayOrder)
            .Select(a => new AmenityResponse(a.Amenity.Id, Localized.Pick(lang, a.Amenity.NameVi, a.Amenity.NameJa)!, a.Amenity.Icon)).ToList();
        var media = x.RoomTypeMedia.OrderBy(m => m.DisplayOrder).ThenBy(m => m.CreatedAt)
            .Select(m => PublicMapping.Image(m.MediaFile, lang)!).ToList();
        return new RoomTypeDetailResponse(x.Id, x.Code, x.Slug, Localized.Pick(lang, x.NameVi, x.NameJa)!,
            Localized.Pick(lang, x.ShortDescriptionVi, x.ShortDescriptionJa), Localized.Pick(lang, x.DescriptionVi, x.DescriptionJa),
            x.Capacity, x.AreaSquareMeters, Localized.Pick(lang, x.BedDescriptionVi, x.BedDescriptionJa), x.PriceDisplayMode,
            x.PriceDisplayMode == PriceDisplayMode.SHOW_PRICE ? x.PriceVnd : null,
            x.PriceDisplayMode == PriceDisplayMode.SHOW_PRICE ? x.PriceUsd : null, PublicMapping.Image(x.FeaturedMedia, lang), amenities, media,
            new SeoResponse(Localized.Pick(lang, x.SeoTitleVi, x.SeoTitleJa), Localized.Pick(lang, x.SeoDescriptionVi, x.SeoDescriptionJa)));
    }
}
