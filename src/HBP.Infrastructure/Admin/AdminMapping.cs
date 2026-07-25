using HBP.Application.Admin;
using HBP.Application.Common;
using HBP.Domain.Entities;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Admin;

internal static class AdminMapping
{
    public static AdminMediaSummary? Media(MediaFile? media) => media is null ? null :
        new AdminMediaSummary(media.Id, media.PublicUrl, MediaUrl.Variant(media.PublicUrl, "medium"),
            MediaUrl.Variant(media.PublicUrl, "thumbnail"), media.AltTextVi, media.AltTextJa);

    public static AdminEmailDeliveryResponse EmailDelivery(EmailDelivery x) =>
        new(x.Id, x.EmailType, x.Recipient, x.LanguageCode, x.Status, x.AttemptCount, x.NextRetryAt,
            x.LastAttemptAt, x.SentAt, x.LastError, x.CreatedAt);

    /// <summary>Loads the deliveries enqueued for a booking/contact request (no FK — resolved here).</summary>
    public static async Task<IReadOnlyList<AdminEmailDeliveryResponse>> LoadDeliveriesAsync(HbpDbContext db,
        string entityType, Guid entityId, CancellationToken cancellationToken) =>
        (await db.EmailDeliveries.AsNoTracking()
            .Where(x => x.RelatedEntityType == entityType && x.RelatedEntityId == entityId)
            .OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken))
        .Select(EmailDelivery).ToList();
}
