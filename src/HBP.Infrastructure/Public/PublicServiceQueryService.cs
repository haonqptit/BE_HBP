using HBP.Application.Abstractions;
using HBP.Application.Common;
using HBP.Application.Public;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Public;

public sealed class PublicServiceQueryService(HbpDbContext db, IRequestLanguageAccessor languageAccessor) : IPublicServiceQueryService
{
    public async Task<IReadOnlyList<ServiceListItemResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var lang = languageAccessor.Language;
        var rows = await db.Services.AsNoTracking().Include(x => x.FeaturedMedia).Where(x => x.IsVisible)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.NameVi).ToListAsync(cancellationToken);
        return rows.Select(x => new ServiceListItemResponse(x.Id, x.Slug, Localized.Pick(lang, x.NameVi, x.NameJa)!,
            Localized.Pick(lang, x.ShortDescriptionVi, x.ShortDescriptionJa), Localized.Pick(lang, x.PriceNoteVi, x.PriceNoteJa),
            PublicMapping.Image(x.FeaturedMedia, lang), x.DisplayOrder)).ToList();
    }

    public async Task<ServiceDetailResponse> GetAsync(string slug, CancellationToken cancellationToken)
    {
        var lang = languageAccessor.Language;
        var x = await db.Services.AsNoTracking().Include(s => s.FeaturedMedia)
            .SingleOrDefaultAsync(s => s.Slug == slug && s.IsVisible, cancellationToken)
            ?? throw new NotFoundException("Service not found.");
        return new ServiceDetailResponse(x.Id, x.Slug, Localized.Pick(lang, x.NameVi, x.NameJa)!,
            Localized.Pick(lang, x.ShortDescriptionVi, x.ShortDescriptionJa), Localized.Pick(lang, x.DescriptionVi, x.DescriptionJa),
            Localized.Pick(lang, x.PriceNoteVi, x.PriceNoteJa), PublicMapping.Image(x.FeaturedMedia, lang));
    }
}
