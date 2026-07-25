using HBP.Application.Common;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Admin;

internal static class FeaturedMediaGuard
{
    public const int MinWidth = 1200;
    public const int MinHeight = 800;

    /// <summary>
    /// FR-MEDIA: the minimum size applies to featured images only, so it is enforced when a media
    /// file is assigned rather than at upload time (gallery and detail images have no size floor).
    /// </summary>
    public static async Task EnsureUsableAsFeaturedAsync(HbpDbContext db, Guid? mediaId, string field,
        CancellationToken cancellationToken)
    {
        if (mediaId is null) return;
        var size = await db.MediaFiles.AsNoTracking().Where(x => x.Id == mediaId.Value)
            .Select(x => new { x.Width, x.Height }).SingleOrDefaultAsync(cancellationToken);
        if (size is null)
            throw new ValidationException("Unknown media file.",
                new Dictionary<string, string[]> { [field] = ["Media file not found."] });
        if (size.Width < MinWidth || size.Height < MinHeight)
            throw new ValidationException("Featured image is too small.",
                new Dictionary<string, string[]>
                {
                    [field] = [$"A featured image must be at least {MinWidth}x{MinHeight} pixels."]
                });
    }
}
