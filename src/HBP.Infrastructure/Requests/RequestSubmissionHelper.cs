using System.Text.Json;
using HBP.Domain.Entities;
using HBP.Domain.Enums;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HBP.Infrastructure.Requests;

internal static class RequestSubmissionHelper
{
    public static async Task AddDeliveriesAsync(HbpDbContext db, string entityType, Guid entityId,
        string adminType, string guestType, string guestEmail, LanguageCode guestLanguage,
        ILogger logger, CancellationToken cancellationToken)
    {
        var value = await db.SystemSettings.AsNoTracking().Where(x => x.Key == "notification_emails")
            .Select(x => x.Value).SingleOrDefaultAsync(cancellationToken);
        string[] recipients;
        try { recipients = JsonSerializer.Deserialize<string[]>(value ?? "[]") ?? []; }
        catch (JsonException) { recipients = []; }
        if (recipients.Length == 0) logger.LogWarning("notification_emails is not configured");
        foreach (var recipient in recipients.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant()).Distinct())
            db.EmailDeliveries.Add(Create(entityType, entityId, adminType, recipient, LanguageCode.Vi));
        db.EmailDeliveries.Add(Create(entityType, entityId, guestType, guestEmail, guestLanguage));
    }

    private static EmailDelivery Create(string entityType, Guid entityId, string emailType, string recipient, LanguageCode language) =>
        new() { RelatedEntityType = entityType, RelatedEntityId = entityId, EmailType = emailType,
            Recipient = recipient, LanguageCode = language, Status = EmailStatus.PENDING };
}
