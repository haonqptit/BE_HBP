using System.Text.Json;
using HBP.Application.Abstractions;
using HBP.Application.Common;
using HBP.Application.Public;
using HBP.Domain.Enums;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Public;

public sealed class PublicSiteMetadataQueryService(
    HbpDbContext db,
    IRequestLanguageAccessor languageAccessor) : IPublicSiteMetadataQueryService
{
    public async Task<SiteMetadataResponse> GetAsync(CancellationToken cancellationToken)
    {
        var value = await db.SystemSettings.AsNoTracking()
            .Where(x => x.Key == "site_metadata")
            .Select(x => x.Value)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(value))
            throw new NotFoundException("Public site metadata was not configured.");

        try
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;
            var language = languageAccessor.Language;
            return new SiteMetadataResponse(
                Required(root, "name"),
                LocalizedRequired(root, language, "addressVi", "addressJa"),
                Required(root, "phone"),
                Required(root, "email"),
                LocalizedRequired(root, language, "checkInVi", "checkInJa"),
                LocalizedRequired(root, language, "checkOutVi", "checkOutJa"),
                LocalizedRequired(root, language, "receptionVi", "receptionJa"));
        }
        catch (JsonException exception)
        {
            throw Invalid($"Invalid site metadata JSON: {exception.Message}");
        }
    }

    private static string Required(JsonElement root, string name)
    {
        if (root.TryGetProperty(name, out var property) &&
            property.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(property.GetString()))
            return property.GetString()!;
        throw Invalid($"Missing required property '{name}'.");
    }

    private static string LocalizedRequired(JsonElement root, LanguageCode language, string viName, string jaName)
    {
        var vi = String(root, viName);
        var ja = String(root, jaName);
        return Localized.Pick(language, vi, ja)
            ?? throw Invalid($"Missing required property '{viName}'.");
    }

    private static string? String(JsonElement root, string name) =>
        root.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static ValidationException Invalid(string message) =>
        new("Invalid public site metadata.",
            new Dictionary<string, string[]> { ["siteMetadata"] = [message] });
}
