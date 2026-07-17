using HBP.Application.Abstractions;
using HBP.Application.Common;
using HBP.Application.Public;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Public;

public sealed class PublicAmenityQueryService(HbpDbContext db, IRequestLanguageAccessor languageAccessor) : IPublicAmenityQueryService
{
    public async Task<IReadOnlyList<AmenityResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var lang = languageAccessor.Language;
        var rows = await db.Amenities.AsNoTracking().Where(x => x.IsVisible)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.NameVi).ToListAsync(cancellationToken);
        return rows.Select(x => new AmenityResponse(x.Id, Localized.Pick(lang, x.NameVi, x.NameJa)!, x.Icon)).ToList();
    }
}
