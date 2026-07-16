using HBP.Application.Abstractions;
using HBP.Application.Common;
using HBP.Application.Media;
using HBP.Domain.Entities;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;

namespace HBP.Infrastructure.Media;

public sealed class MediaService(HbpDbContext db, IImageProcessor processor, IMediaStorage storage) : IMediaService
{
    private static readonly HashSet<string> AllowedTypes = ["image/jpeg", "image/png", "image/webp"];

    public async Task<MediaResponse> UploadAsync(Stream stream, string fileName, string contentType, long length,
        string? altTextVi, string? altTextJa, CancellationToken cancellationToken)
    {
        if (length <= 0 || length > 5 * 1024 * 1024) throw new ValidationException("Image must be between 1 byte and 5 MB.");
        if (!AllowedTypes.Contains(contentType.ToLowerInvariant())) throw new ValidationException("Unsupported image type.");
        var id = Guid.NewGuid();
        ProcessedImage image;
        try { image = await processor.ProcessAsync(stream, cancellationToken); }
        catch (UnknownImageFormatException) { throw new ValidationException("The uploaded file is not a valid image."); }
        var paths = await storage.SaveAsync(id, image, cancellationToken);
        var entity = new MediaFile { Id = id, OriginalFileName = Path.GetFileName(fileName), StoredFileName = "original.webp",
            StoragePath = paths.Original, PublicUrl = paths.PublicUrl, MimeType = "image/webp", SizeBytes = image.Original.LongLength,
            Width = image.Width, Height = image.Height, AltTextVi = altTextVi, AltTextJa = altTextJa };
        db.MediaFiles.Add(entity);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch { await storage.DeleteAsync(paths, cancellationToken); throw; }
        return Map(entity);
    }

    public async Task<PagedResult<MediaResponse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.MediaFiles.AsNoTracking().OrderByDescending(x => x.CreatedAt);
        return new PagedResult<MediaResponse>(await query.Skip((page - 1) * pageSize).Take(pageSize).Select(x => Map(x)).ToListAsync(cancellationToken),
            page, pageSize, await query.LongCountAsync(cancellationToken));
    }

    public async Task<MediaResponse> GetAsync(Guid id, CancellationToken cancellationToken) => Map(
        await db.MediaFiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Media not found."));

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.MediaFiles.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Media not found.");
        var refs = new List<string>();
        if (await db.RoomTypes.AnyAsync(x => x.FeaturedMediaId == id, cancellationToken)) refs.Add("room_types.featured_media_id");
        if (await db.Services.AnyAsync(x => x.FeaturedMediaId == id, cancellationToken)) refs.Add("services.featured_media_id");
        if (await db.RoomTypeMedia.AnyAsync(x => x.MediaFileId == id, cancellationToken)) refs.Add("room_type_media");
        if (await db.GalleryItems.AnyAsync(x => x.MediaFileId == id, cancellationToken)) refs.Add("gallery_items");
        if (refs.Count > 0) throw new MediaInUseException(refs);
        db.MediaFiles.Remove(entity); await db.SaveChangesAsync(cancellationToken);
        var directory = Path.GetDirectoryName(entity.StoragePath)!;
        await storage.DeleteAsync(new StoredMediaPaths(entity.StoragePath, Path.Combine(directory, "medium.webp"), Path.Combine(directory, "thumbnail.webp"), entity.PublicUrl), cancellationToken);
    }

    private static MediaResponse Map(MediaFile x) => new(x.Id, x.OriginalFileName, x.PublicUrl, x.MimeType, x.SizeBytes, x.Width, x.Height, x.AltTextVi, x.AltTextJa, x.CreatedAt);
}
