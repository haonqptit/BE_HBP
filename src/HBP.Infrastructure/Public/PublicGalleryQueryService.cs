using HBP.Application.Abstractions;
using HBP.Application.Common;
using HBP.Application.Public;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Public;

public sealed class PublicGalleryQueryService(HbpDbContext db, IRequestLanguageAccessor languageAccessor) : IPublicGalleryQueryService
{
    public async Task<IReadOnlyList<GalleryCategoryResponse>> ListAsync(string? category, CancellationToken cancellationToken)
    {
        var lang = languageAccessor.Language;
        var query = db.GalleryCategories.AsNoTracking().AsSplitQuery().Include(x => x.GalleryItems)
            .ThenInclude(x => x.MediaFile).Where(x => x.IsVisible);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Slug == category);
        var rows = await query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.NameVi).ToListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(category) && rows.Count == 0) throw new NotFoundException("Gallery category not found.");
        return rows.Select(x => new GalleryCategoryResponse(x.Id, x.Slug, Localized.Pick(lang, x.NameVi, x.NameJa)!, x.DisplayOrder,
            x.GalleryItems.Where(i => i.IsVisible).OrderBy(i => i.DisplayOrder).ThenBy(i => i.CreatedAt)
                .Select(i => new GalleryItemResponse(i.Id, PublicMapping.Image(i.MediaFile, lang)!,
                    Localized.Pick(lang, i.CaptionVi, i.CaptionJa), i.DisplayOrder)).ToList())).ToList();
    }
}
