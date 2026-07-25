using HBP.Application.Admin;
using HBP.Application.Common;
using HBP.Domain.Entities;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Admin;

public sealed class AdminAmenityService(HbpDbContext db) : IAdminAmenityService
{
    public async Task<PagedResult<AdminAmenityResponse>> ListAsync(PageQuery query, CancellationToken cancellationToken)
    {
        var amenities = db.Amenities.AsNoTracking().AsQueryable();
        var search = query.TrimmedSearch;
        if (search is not null)
            amenities = amenities.Where(x => EF.Functions.ILike(x.NameVi, $"%{search}%")
                || (x.NameJa != null && EF.Functions.ILike(x.NameJa, $"%{search}%")));
        amenities = query.NormalizedSort switch
        {
            "name" => amenities.OrderBy(x => x.NameVi),
            "name_desc" => amenities.OrderByDescending(x => x.NameVi),
            "created_at" => amenities.OrderBy(x => x.CreatedAt),
            "created_at_desc" => amenities.OrderByDescending(x => x.CreatedAt),
            _ => amenities.OrderBy(x => x.DisplayOrder).ThenBy(x => x.NameVi)
        };
        return await AdminPaging.ToPagedResultAsync(amenities, query, Map, cancellationToken);
    }

    public async Task<AdminAmenityResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await db.Amenities.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Amenity not found."));

    public async Task<AdminAmenityResponse> CreateAsync(SaveAmenityRequest request, CancellationToken cancellationToken)
    {
        var entity = new Amenity();
        Apply(entity, request);
        db.Amenities.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<AdminAmenityResponse> UpdateAsync(Guid id, SaveAmenityRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Amenities.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Amenity not found.");
        Apply(entity, request);
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Amenities.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Amenity not found.");
        db.Amenities.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(Amenity entity, SaveAmenityRequest request)
    {
        entity.NameVi = request.NameVi.Trim();
        entity.NameJa = request.NameJa?.Trim();
        entity.Icon = request.Icon?.Trim();
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsVisible = request.IsVisible;
    }

    private static AdminAmenityResponse Map(Amenity x) =>
        new(x.Id, x.NameVi, x.NameJa, x.Icon, x.DisplayOrder, x.IsVisible, x.CreatedAt, x.UpdatedAt);
}
