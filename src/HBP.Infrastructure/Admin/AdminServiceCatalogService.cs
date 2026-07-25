using HBP.Application.Admin;
using HBP.Application.Common;
using HBP.Domain.Entities;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Admin;

public sealed class AdminServiceCatalogService(HbpDbContext db) : IAdminServiceCatalogService
{
    public async Task<PagedResult<AdminServiceResponse>> ListAsync(PageQuery query, CancellationToken cancellationToken)
    {
        var services = db.Services.AsNoTracking().Include(x => x.FeaturedMedia).AsQueryable();
        var search = query.TrimmedSearch;
        if (search is not null)
            services = services.Where(x => EF.Functions.ILike(x.NameVi, $"%{search}%")
                || EF.Functions.ILike(x.Slug, $"%{search}%"));
        services = query.NormalizedSort switch
        {
            "name" => services.OrderBy(x => x.NameVi),
            "name_desc" => services.OrderByDescending(x => x.NameVi),
            "created_at" => services.OrderBy(x => x.CreatedAt),
            "created_at_desc" => services.OrderByDescending(x => x.CreatedAt),
            _ => services.OrderBy(x => x.DisplayOrder).ThenBy(x => x.NameVi)
        };
        return await AdminPaging.ToPagedResultAsync(services, query, Map, cancellationToken);
    }

    public async Task<AdminServiceResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await LoadAsync(id, tracking: false, cancellationToken));

    public async Task<AdminServiceResponse> CreateAsync(SaveServiceRequest request, CancellationToken cancellationToken)
    {
        await EnsureUniqueAsync(request, null, cancellationToken);
        await FeaturedMediaGuard.EnsureUsableAsFeaturedAsync(db, request.FeaturedMediaId, "featuredMediaId", cancellationToken);
        var entity = new Service();
        Apply(entity, request);
        db.Services.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Map(await LoadAsync(entity.Id, tracking: false, cancellationToken));
    }

    public async Task<AdminServiceResponse> UpdateAsync(Guid id, SaveServiceRequest request, CancellationToken cancellationToken)
    {
        var entity = await LoadAsync(id, tracking: true, cancellationToken);
        await EnsureUniqueAsync(request, id, cancellationToken);
        await FeaturedMediaGuard.EnsureUsableAsFeaturedAsync(db, request.FeaturedMediaId, "featuredMediaId", cancellationToken);
        Apply(entity, request);
        await db.SaveChangesAsync(cancellationToken);
        return Map(await LoadAsync(id, tracking: false, cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Services.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Service not found.");
        db.Services.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Service> LoadAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<Service> query = tracking ? db.Services : db.Services.AsNoTracking();
        return await query.Include(x => x.FeaturedMedia).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Service not found.");
    }

    private async Task EnsureUniqueAsync(SaveServiceRequest request, Guid? id, CancellationToken cancellationToken)
    {
        var others = id is null ? db.Services : db.Services.Where(x => x.Id != id.Value);
        if (await others.AnyAsync(x => x.Slug == request.Slug, cancellationToken))
            throw new ConflictException($"Service slug '{request.Slug}' is already in use.");
    }

    private static void Apply(Service entity, SaveServiceRequest request)
    {
        entity.Slug = request.Slug.Trim();
        entity.NameVi = request.NameVi.Trim();
        entity.NameJa = request.NameJa?.Trim();
        entity.ShortDescriptionVi = request.ShortDescriptionVi?.Trim();
        entity.ShortDescriptionJa = request.ShortDescriptionJa?.Trim();
        entity.DescriptionVi = request.DescriptionVi;
        entity.DescriptionJa = request.DescriptionJa;
        entity.PriceNoteVi = request.PriceNoteVi?.Trim();
        entity.PriceNoteJa = request.PriceNoteJa?.Trim();
        entity.FeaturedMediaId = request.FeaturedMediaId;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsVisible = request.IsVisible;
    }

    private static AdminServiceResponse Map(Service x) => new(x.Id, x.Slug, x.NameVi, x.NameJa,
        x.ShortDescriptionVi, x.ShortDescriptionJa, x.DescriptionVi, x.DescriptionJa,
        x.PriceNoteVi, x.PriceNoteJa, x.FeaturedMediaId, AdminMapping.Media(x.FeaturedMedia),
        x.DisplayOrder, x.IsVisible, x.CreatedAt, x.UpdatedAt);
}
