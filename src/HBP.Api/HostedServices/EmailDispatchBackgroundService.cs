using System.Text.Json;
using HBP.Application.Abstractions;
using HBP.Application.Common;
using HBP.Application.Email;
using HBP.Domain.Entities;
using HBP.Domain.Enums;
using HBP.Infrastructure.Email;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HBP.Api.HostedServices;

public sealed class EmailDispatchBackgroundService(IServiceScopeFactory scopeFactory,
    IOptions<EmailDispatchOptions> dispatchOptions, IOptions<SmtpOptions> smtpOptions,
    IClock clock, ILogger<EmailDispatchBackgroundService> logger) : BackgroundService
{
    private DateTimeOffset _lastRetention = DateTimeOffset.MinValue;
    private bool _smtpWarningLogged;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, dispatchOptions.Value.PollIntervalSeconds)));
        do
        {
            try
            {
                if (string.IsNullOrWhiteSpace(smtpOptions.Value.Host))
                {
                    if (!_smtpWarningLogged) { logger.LogWarning("SMTP host is not configured; email worker is idle"); _smtpWarningLogged = true; }
                }
                else await ProcessBatchAsync(stoppingToken);
                await RunRetentionAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Email dispatch loop failed"); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HbpDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var renderer = scope.ServiceProvider.GetRequiredService<IEmailTemplateRenderer>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var batchSize = Math.Clamp(dispatchOptions.Value.BatchSize, 1, 100);
        var ids = await db.Database.SqlQuery<Guid>($"""
            SELECT id AS "Value" FROM email_deliveries
            WHERE status = 'PENDING' OR (status = 'RETRYING' AND next_retry_at <= now())
            ORDER BY created_at LIMIT {batchSize} FOR UPDATE SKIP LOCKED
            """).ToListAsync(cancellationToken);
        var deliveries = await db.EmailDeliveries.Where(x => ids.Contains(x.Id)).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        foreach (var delivery in deliveries)
        {
            var now = clock.UtcNow.UtcDateTime;
            try
            {
                var model = await BuildModelAsync(db, delivery, cancellationToken);
                if (model is null) { delivery.Status = EmailStatus.FAILED; delivery.LastError = "Related entity missing"; continue; }
                var rendered = await renderer.RenderAsync(delivery.EmailType, delivery.LanguageCode, model, cancellationToken);
                await sender.SendAsync(delivery.Recipient, rendered.Subject, rendered.HtmlBody, cancellationToken);
                delivery.Status = EmailStatus.SENT; delivery.SentAt = now; delivery.LastAttemptAt = now;
                delivery.NextRetryAt = null; delivery.LastError = null;
            }
            catch (Exception exception)
            {
                delivery.AttemptCount++; delivery.LastAttemptAt = now;
                delivery.LastError = exception.Message[..Math.Min(1000, exception.Message.Length)];
                var delay = EmailBackoff.ForAttempt(delivery.AttemptCount);
                if (delivery.AttemptCount >= dispatchOptions.Value.MaxAttempts || delay is null)
                { delivery.Status = EmailStatus.FAILED; delivery.NextRetryAt = null; }
                else { delivery.Status = EmailStatus.RETRYING; delivery.NextRetryAt = now.Add(delay.Value); }
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<string, object?>?> BuildModelAsync(HbpDbContext db,
        EmailDelivery delivery, CancellationToken cancellationToken)
    {
        var siteName = "HBP";
        string? companyAddress = null;
        string? companyPhone = null;
        string? companyEmail = null;
        var metadata = await db.SystemSettings.AsNoTracking().Where(x => x.Key == "site_metadata").Select(x => x.Value).SingleOrDefaultAsync(cancellationToken);
        try
        {
            if (metadata is not null)
            {
                using var document = JsonDocument.Parse(metadata);
                var root = document.RootElement;
                if (root.TryGetProperty("name", out var name)) siteName = name.GetString() ?? siteName;
                var addressProperty = delivery.LanguageCode == LanguageCode.Ja ? "addressJa" : "addressVi";
                if (root.TryGetProperty(addressProperty, out var address)) companyAddress = address.GetString();
                if (root.TryGetProperty("phone", out var phone)) companyPhone = phone.GetString();
                if (root.TryGetProperty("email", out var email)) companyEmail = email.GetString();
            }
        }
        catch (JsonException) { }

        Dictionary<string, object?> CommonModel() => new()
        {
            ["site_name"] = siteName,
            ["company_name"] = siteName,
            ["related_entity_id"] = delivery.RelatedEntityId
        };

        if (delivery.RelatedEntityType == "BookingRequest")
        {
            var x = await db.BookingRequests.AsNoTracking().Include(b => b.RoomType).SingleOrDefaultAsync(b => b.Id == delivery.RelatedEntityId, cancellationToken);
            if (x is null) return null;
            var model = CommonModel();
            if (!string.IsNullOrWhiteSpace(companyAddress)) model["company_address"] = companyAddress;
            if (!string.IsNullOrWhiteSpace(companyPhone)) model["company_phone"] = companyPhone;
            if (!string.IsNullOrWhiteSpace(companyEmail)) model["company_email"] = companyEmail;
            model["reference_code"] = x.ReferenceCode;
            model["full_name"] = x.FullName;
            model["email"] = x.Email;
            model["phone_number"] = x.PhoneNumber;
            model["check_in_date"] = x.CheckInDate?.ToString("dd/MM/yyyy");
            model["check_out_date"] = x.CheckOutDate?.ToString("dd/MM/yyyy");
            model["adults"] = x.Adults;
            model["children"] = x.Children;
            model["number_of_rooms"] = x.NumberOfRooms;
            model["room_type_name"] = x.RoomType is null ? null : Localized.Pick(delivery.LanguageCode, x.RoomType.NameVi, x.RoomType.NameJa);
            model["customer_message"] = x.CustomerMessage;
            return model;
        }
        if (delivery.RelatedEntityType == "ContactRequest")
        {
            var x = await db.ContactRequests.AsNoTracking().SingleOrDefaultAsync(c => c.Id == delivery.RelatedEntityId, cancellationToken);
            if (x is null) return null;
            var model = CommonModel();
            if (!string.IsNullOrWhiteSpace(companyAddress)) model["company_address"] = companyAddress;
            if (!string.IsNullOrWhiteSpace(companyPhone)) model["company_phone"] = companyPhone;
            if (!string.IsNullOrWhiteSpace(companyEmail)) model["company_email"] = companyEmail;
            model["reference_code"] = x.ReferenceCode;
            model["full_name"] = x.FullName;
            model["email"] = x.Email;
            model["phone_number"] = x.PhoneNumber;
            model["subject"] = x.Subject;
            model["message"] = x.Message;
            return model;
        }
        return null;
    }

    private async Task RunRetentionAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        if (now - _lastRetention < TimeSpan.FromHours(24)) return;
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HbpDbContext>();
        var cutoff = now.UtcDateTime.AddDays(-dispatchOptions.Value.RetentionDays);
        await db.EmailDeliveries.Where(x => x.CreatedAt < cutoff).ExecuteDeleteAsync(cancellationToken);
        _lastRetention = now;
    }
}
