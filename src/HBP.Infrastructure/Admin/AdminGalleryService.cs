using HBP.Application.Admin;
using HBP.Application.Common;
using HBP.Domain.Entities;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Admin;

public sealed class AdminGalleryService(HbpDbContext db) : IAdminGalleryService
{
    public async Task<PagedResult<AdminGalleryCategoryResponse>> ListCategoriesAsync(PageQuery query, CancellationToken cancellationToken)
    {
        var categories = db.GalleryCategories.AsNoTracking().Include(x => x.GalleryItems).AsQueryable();
        var search = query.TrimmedSearch;
        if (search is not null)
            categories = categories.Where(x => EF.Functions.ILike(x.NameVi, $"%{search}%")
                || EF.Functions.ILike(x.Slug, $"%{search}%"));
        categories = query.NormalizedSort switch
        {
            "name" => categories.OrderBy(x => x.NameVi),
            "name_desc" => categories.OrderByDescending(x => x.NameVi),
            "created_at" => categories.OrderBy(x => x.CreatedAt),
            "created_at_desc" => categories.OrderByDescending(x => x.CreatedAt),
            _ => categories.OrderBy(x => x.DisplayOrder).ThenBy(x => x.NameVi)
        };
        return await AdminPaging.ToPagedResultAsync(categories, query, MapCategory, cancellationToken);
    }

    public async Task<AdminGalleryCategoryResponse> GetCategoryAsync(Guid id, CancellationToken cancellationToken) =>
        MapCategory(await db.GalleryCategories.AsNoTracking().Include(x => x.GalleryItems)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Gallery category not found."));

    public async Task<AdminGalleryCategoryResponse> CreateCategoryAsync(SaveGalleryCategoryRequest request, CancellationToken cancellationToken)
    {
        await EnsureCategoryUniqueAsync(request, null, cancellationToken);
        var entity = new GalleryCategory();
        ApplyCategory(entity, request);
        db.GalleryCategories.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return MapCategory(entity);
    }

    public async Task<AdminGalleryCategoryResponse> UpdateCategoryAsync(Guid id, SaveGalleryCategoryRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.GalleryCategories.Include(x => x.GalleryItems)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Gallery category not found.");
        await EnsureCategoryUniqueAsync(request, id, cancellationToken);
        ApplyCategory(entity, request);
        await db.SaveChangesAsync(cancellationToken);
        return MapCategory(entity);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.GalleryCategories.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Gallery category not found.");
        db.GalleryCategories.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AdminGalleryItemResponse>> ListItemsAsync(Guid? categoryId, PageQuery query, CancellationToken cancellationToken)
    {
        var items = db.GalleryItems.AsNoTracking().Include(x => x.MediaFile).Include(x => x.GalleryCategory).AsQueryable();
        if (categoryId is not null) items = items.Where(x => x.GalleryCategoryId == categoryId.Value);
        var search = query.TrimmedSearch;
        if (search is not null)
            items = items.Where(x => (x.CaptionVi != null && EF.Functions.ILike(x.CaptionVi, $"%{search}%"))
                || EF.Functions.ILike(x.MediaFile.OriginalFileName, $"%{search}%"));
        items = query.NormalizedSort switch
        {
            "created_at" => items.OrderBy(x => x.CreatedAt),
            "created_at_desc" => items.OrderByDescending(x => x.CreatedAt),
            _ => items.OrderBy(x => x.GalleryCategory.DisplayOrder).ThenBy(x => x.DisplayOrder).ThenBy(x => x.CreatedAt)
        };
        return await AdminPaging.ToPagedResultAsync(items, query, MapItem, cancellationToken);
    }

    public async Task<AdminGalleryItemResponse> GetItemAsync(Guid id, CancellationToken cancellationToken) =>
        MapItem(await LoadItemAsync(id, tracking: false, cancellationToken));

    public async Task<AdminGalleryItemResponse> CreateItemAsync(SaveGalleryItemRequest request, CancellationToken cancellationToken)
    {
        await EnsureItemReferencesAsync(request, cancellationToken);
        var entity = new GalleryItem();
        ApplyItem(entity, request);
        db.GalleryItems.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return MapItem(await LoadItemAsync(entity.Id, tracking: false, cancellationToken));
    }

    public async Task<AdminGalleryItemResponse> UpdateItemAsync(Guid id, SaveGalleryItemRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.GalleryItems.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Gallery item not found.");
        await EnsureItemReferencesAsync(request, cancellationToken);
        ApplyItem(entity, request);
        await db.SaveChangesAsync(cancellationToken);
        return MapItem(await LoadItemAsync(id, tracking: false, cancellationToken));
    }

    public async Task DeleteItemAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.GalleryItems.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Gallery item not found.");
        db.GalleryItems.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<GalleryItem> LoadItemAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<GalleryItem> query = tracking ? db.GalleryItems : db.GalleryItems.AsNoTracking();
        return await query.Include(x => x.MediaFile).Include(x => x.GalleryCategory)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Gallery item not found.");
    }

    private async Task EnsureCategoryUniqueAsync(SaveGalleryCategoryRequest request, Guid? id, CancellationToken cancellationToken)
    {
        var others = id is null ? db.GalleryCategories : db.GalleryCategories.Where(x => x.Id != id.Value);
        if (await others.AnyAsync(x => x.Slug == request.Slug, cancellationToken))
            throw new ConflictException($"Gallery category slug '{request.Slug}' is already in use.");
    }

    private async Task EnsureItemReferencesAsync(SaveGalleryItemRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (!await db.GalleryCategories.AnyAsync(x => x.Id == request.GalleryCategoryId, cancellationToken))
            errors["galleryCategoryId"] = ["Gallery category not found."];
        if (!await db.MediaFiles.AnyAsync(x => x.Id == request.MediaFileId, cancellationToken))
            errors["mediaFileId"] = ["Media file not found."];
        if (errors.Count > 0) throw new ValidationException("Unknown gallery item reference.", errors);
    }

    private static void ApplyCategory(GalleryCategory entity, SaveGalleryCategoryRequest request)
    {
        entity.Slug = request.Slug.Trim();
        entity.NameVi = request.NameVi.Trim();
        entity.NameJa = request.NameJa?.Trim();
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsVisible = request.IsVisible;
    }

    private static void ApplyItem(GalleryItem entity, SaveGalleryItemRequest request)
    {
        entity.GalleryCategoryId = request.GalleryCategoryId;
        entity.MediaFileId = request.MediaFileId;
        entity.CaptionVi = request.CaptionVi?.Trim();
        entity.CaptionJa = request.CaptionJa?.Trim();
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsVisible = request.IsVisible;
    }

    private static AdminGalleryCategoryResponse MapCategory(GalleryCategory x) =>
        new(x.Id, x.Slug, x.NameVi, x.NameJa, x.DisplayOrder, x.IsVisible, x.GalleryItems.Count, x.CreatedAt, x.UpdatedAt);

    private static AdminGalleryItemResponse MapItem(GalleryItem x) =>
        new(x.Id, x.GalleryCategoryId, x.GalleryCategory.Slug, x.MediaFileId, AdminMapping.Media(x.MediaFile)!,
            x.CaptionVi, x.CaptionJa, x.DisplayOrder, x.IsVisible, x.CreatedAt, x.UpdatedAt);
}
