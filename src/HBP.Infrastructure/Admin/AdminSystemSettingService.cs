using System.Text.RegularExpressions;
using HBP.Application.Admin;
using HBP.Application.Common;
using HBP.Domain.Entities;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Admin;

public sealed partial class AdminSystemSettingService(HbpDbContext db) : IAdminSystemSettingService
{
    /// <summary>
    /// Credentials live in environment variables, never in <c>system_settings</c>. Any key that looks
    /// like a secret is therefore neither readable nor writable through the admin API.
    /// </summary>
    private static readonly string[] SecretMarkers = ["secret", "password", "token", "credential", "api_key"];

    public async Task<IReadOnlyList<AdminSystemSettingResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await db.SystemSettings.AsNoTracking().OrderBy(x => x.Key).ToListAsync(cancellationToken))
        .Where(x => !IsSecret(x.Key)).Select(Map).ToList();

    public async Task<AdminSystemSettingResponse> GetAsync(string key, CancellationToken cancellationToken)
    {
        var normalized = Normalize(key);
        return Map(await db.SystemSettings.AsNoTracking().SingleOrDefaultAsync(x => x.Key == normalized, cancellationToken)
            ?? throw new NotFoundException($"Setting '{normalized}' not found."));
    }

    public async Task<AdminSystemSettingResponse> UpdateAsync(string key, UpdateSystemSettingRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = Normalize(key);
        var entity = await db.SystemSettings.SingleOrDefaultAsync(x => x.Key == normalized, cancellationToken);
        if (entity is null)
        {
            entity = new SystemSetting { Key = normalized };
            db.SystemSettings.Add(entity);
        }
        entity.Value = request.Value;
        if (request.Description is not null) entity.Description = request.Description.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    private static string Normalize(string key)
    {
        var normalized = key.Trim().ToLowerInvariant();
        if (!KeyPattern().IsMatch(normalized))
            throw new ValidationException("Invalid setting key.",
                new Dictionary<string, string[]> { ["key"] = ["Key must be 1-100 characters of a-z, 0-9, '_' or '.'."] });
        if (IsSecret(normalized))
            throw new ValidationException("Secret settings are managed through environment variables.",
                new Dictionary<string, string[]> { ["key"] = ["This key is reserved for environment configuration."] });
        return normalized;
    }

    private static bool IsSecret(string key) => SecretMarkers.Any(marker => key.Contains(marker, StringComparison.Ordinal));

    private static AdminSystemSettingResponse Map(SystemSetting x) => new(x.Key, x.Value, x.Description, x.UpdatedAt);

    [GeneratedRegex("^[a-z0-9_.]{1,100}$")]
    private static partial Regex KeyPattern();
}
