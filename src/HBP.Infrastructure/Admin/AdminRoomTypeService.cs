using HBP.Application.Admin;
using HBP.Application.Common;
using HBP.Domain.Entities;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Admin;

public sealed class AdminRoomTypeService(HbpDbContext db) : IAdminRoomTypeService
{
    public async Task<PagedResult<AdminRoomTypeListItem>> ListAsync(PageQuery query, CancellationToken cancellationToken)
    {
        var rooms = db.RoomTypes.AsNoTracking().Include(x => x.FeaturedMedia).AsQueryable();
        var search = query.TrimmedSearch;
        if (search is not null)
            rooms = rooms.Where(x => EF.Functions.ILike(x.NameVi, $"%{search}%")
                || EF.Functions.ILike(x.Code, $"%{search}%")
                || EF.Functions.ILike(x.Slug, $"%{search}%"));
        rooms = query.NormalizedSort switch
        {
            "name" => rooms.OrderBy(x => x.NameVi),
            "name_desc" => rooms.OrderByDescending(x => x.NameVi),
            "code" => rooms.OrderBy(x => x.Code),
            "created_at" => rooms.OrderBy(x => x.CreatedAt),
            "created_at_desc" => rooms.OrderByDescending(x => x.CreatedAt),
            _ => rooms.OrderBy(x => x.DisplayOrder).ThenBy(x => x.NameVi)
        };
        return await AdminPaging.ToPagedResultAsync(rooms, query, x => new AdminRoomTypeListItem(x.Id, x.Code, x.Slug,
            x.NameVi, x.NameJa, x.PriceDisplayMode, x.PriceVnd, x.PriceUsd, x.Capacity, x.DisplayOrder, x.IsVisible,
            AdminMapping.Media(x.FeaturedMedia), x.UpdatedAt), cancellationToken);
    }

    public async Task<AdminRoomTypeResponse> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await LoadAsync(id, tracking: false, cancellationToken));

    public async Task<AdminRoomTypeResponse> CreateAsync(SaveRoomTypeRequest request, CancellationToken cancellationToken)
    {
        await EnsureUniqueAsync(request, null, cancellationToken);
        await FeaturedMediaGuard.EnsureUsableAsFeaturedAsync(db, request.FeaturedMediaId, "featuredMediaId", cancellationToken);
        var entity = new RoomType();
        Apply(entity, request);
        db.RoomTypes.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Map(await LoadAsync(entity.Id, tracking: false, cancellationToken));
    }

    public async Task<AdminRoomTypeResponse> UpdateAsync(Guid id, SaveRoomTypeRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.RoomTypes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Room type not found.");
        await EnsureUniqueAsync(request, id, cancellationToken);
        await FeaturedMediaGuard.EnsureUsableAsFeaturedAsync(db, request.FeaturedMediaId, "featuredMediaId", cancellationToken);
        Apply(entity, request);
        await db.SaveChangesAsync(cancellationToken);
        return Map(await LoadAsync(id, tracking: false, cancellationToken));
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.RoomTypes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Room type not found.");
        db.RoomTypes.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminRoomTypeResponse> ReplaceAmenitiesAsync(Guid id, ReplaceLinksRequest request, CancellationToken cancellationToken)
    {
        var entity = await LoadAsync(id, tracking: true, cancellationToken);
        var items = Distinct(request);
        var missing = await MissingIdsAsync(db.Amenities.Select(x => x.Id), items.Keys, cancellationToken);
        if (missing.Count > 0) throw new ValidationException("Unknown amenity.",
            new Dictionary<string, string[]> { ["items"] = [$"Amenities not found: {string.Join(", ", missing)}"] });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.RoomTypeAmenities.RemoveRange(entity.RoomTypeAmenities);
        // Flush the deletes first: (room_type_id, amenity_id) is the primary key, so re-adding a link
        // that is still tracked as Deleted would collide in the change tracker.
        await db.SaveChangesAsync(cancellationToken);
        foreach (var (amenityId, displayOrder) in items)
            db.RoomTypeAmenities.Add(new RoomTypeAmenity { RoomTypeId = id, AmenityId = amenityId, DisplayOrder = displayOrder });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(await LoadAsync(id, tracking: false, cancellationToken));
    }

    public async Task<AdminRoomTypeResponse> ReplaceMediaAsync(Guid id, ReplaceLinksRequest request, CancellationToken cancellationToken)
    {
        var entity = await LoadAsync(id, tracking: true, cancellationToken);
        var items = Distinct(request);
        var missing = await MissingIdsAsync(db.MediaFiles.Select(x => x.Id), items.Keys, cancellationToken);
        if (missing.Count > 0) throw new ValidationException("Unknown media file.",
            new Dictionary<string, string[]> { ["items"] = [$"Media files not found: {string.Join(", ", missing)}"] });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.RoomTypeMedia.RemoveRange(entity.RoomTypeMedia);
        // Flush the deletes first so the uq_room_type_media unique constraint is not tripped when the
        // same media file is re-inserted with a different order in the same round-trip.
        await db.SaveChangesAsync(cancellationToken);
        foreach (var (mediaFileId, displayOrder) in items)
            db.RoomTypeMedia.Add(new RoomTypeMedia { RoomTypeId = id, MediaFileId = mediaFileId, DisplayOrder = displayOrder ?? 0 });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(await LoadAsync(id, tracking: false, cancellationToken));
    }

    private async Task<RoomType> LoadAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<RoomType> query = tracking ? db.RoomTypes : db.RoomTypes.AsNoTracking();
        query = query.AsSplitQuery().Include(x => x.FeaturedMedia)
            .Include(x => x.RoomTypeAmenities).ThenInclude(x => x.Amenity)
            .Include(x => x.RoomTypeMedia).ThenInclude(x => x.MediaFile);
        return await query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Room type not found.");
    }

    private static Dictionary<Guid, int?> Distinct(ReplaceLinksRequest request)
    {
        var items = new Dictionary<Guid, int?>();
        var index = 0;
        foreach (var item in request.Items)
        {
            // Last write wins on duplicates; a missing displayOrder falls back to the payload position.
            items[item.Id] = item.DisplayOrder ?? index;
            index++;
        }
        return items;
    }

    private static async Task<List<Guid>> MissingIdsAsync(IQueryable<Guid> existing, IEnumerable<Guid> requested,
        CancellationToken cancellationToken)
    {
        var ids = requested.ToList();
        if (ids.Count == 0) return [];
        var found = await existing.Where(x => ids.Contains(x)).ToListAsync(cancellationToken);
        return ids.Except(found).ToList();
    }

    private static IQueryable<RoomType> Others(IQueryable<RoomType> query, Guid? id) =>
        id is null ? query : query.Where(x => x.Id != id.Value);

    private async Task EnsureUniqueAsync(SaveRoomTypeRequest request, Guid? id, CancellationToken cancellationToken)
    {
        if (await Others(db.RoomTypes, id).AnyAsync(x => x.Code == request.Code, cancellationToken))
            throw new ConflictException($"Room type code '{request.Code}' is already in use.");
        if (await Others(db.RoomTypes, id).AnyAsync(x => x.Slug == request.Slug, cancellationToken))
            throw new ConflictException($"Room type slug '{request.Slug}' is already in use.");
    }

    private static void Apply(RoomType entity, SaveRoomTypeRequest request)
    {
        entity.Code = request.Code.Trim();
        entity.Slug = request.Slug.Trim();
        entity.NameVi = request.NameVi.Trim();
        entity.NameJa = request.NameJa?.Trim();
        entity.ShortDescriptionVi = request.ShortDescriptionVi?.Trim();
        entity.ShortDescriptionJa = request.ShortDescriptionJa?.Trim();
        entity.DescriptionVi = request.DescriptionVi;
        entity.DescriptionJa = request.DescriptionJa;
        entity.PriceVnd = request.PriceVnd;
        entity.PriceUsd = request.PriceUsd;
        entity.PriceDisplayMode = request.PriceDisplayMode;
        entity.Capacity = request.Capacity;
        entity.AreaSquareMeters = request.AreaSquareMeters;
        entity.BedDescriptionVi = request.BedDescriptionVi?.Trim();
        entity.BedDescriptionJa = request.BedDescriptionJa?.Trim();
        entity.FeaturedMediaId = request.FeaturedMediaId;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsVisible = request.IsVisible;
        entity.SeoTitleVi = request.SeoTitleVi?.Trim();
        entity.SeoTitleJa = request.SeoTitleJa?.Trim();
        entity.SeoDescriptionVi = request.SeoDescriptionVi?.Trim();
        entity.SeoDescriptionJa = request.SeoDescriptionJa?.Trim();
    }

    private static AdminRoomTypeResponse Map(RoomType x) => new(x.Id, x.Code, x.Slug, x.NameVi, x.NameJa,
        x.ShortDescriptionVi, x.ShortDescriptionJa, x.DescriptionVi, x.DescriptionJa, x.PriceVnd, x.PriceUsd,
        x.PriceDisplayMode, x.Capacity, x.AreaSquareMeters, x.BedDescriptionVi, x.BedDescriptionJa,
        x.FeaturedMediaId, AdminMapping.Media(x.FeaturedMedia), x.DisplayOrder, x.IsVisible,
        x.SeoTitleVi, x.SeoTitleJa, x.SeoDescriptionVi, x.SeoDescriptionJa,
        x.RoomTypeAmenities.OrderBy(a => a.DisplayOrder ?? 0).ThenBy(a => a.Amenity.NameVi)
            .Select(a => new AdminRoomTypeAmenityLink(a.AmenityId, a.Amenity.NameVi, a.Amenity.NameJa, a.DisplayOrder)).ToList(),
        x.RoomTypeMedia.OrderBy(m => m.DisplayOrder).ThenBy(m => m.CreatedAt)
            .Select(m => new AdminRoomTypeMediaLink(m.MediaFileId, m.DisplayOrder, AdminMapping.Media(m.MediaFile)!)).ToList(),
        x.CreatedAt, x.UpdatedAt);
}
