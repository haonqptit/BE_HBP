using System.Text.Json;
using HBP.Application.Abstractions;
using HBP.Domain.Entities;
using HBP.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Persistence.Seed;

internal static class FrontendContentSeeder
{
    private const string MediaPrefix = "frontend-seed--";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly string[] RoomImages = ["cover.png", "01.png", "02.png", "03.png", "04.png", "05.png"];
    private static readonly string[] ServiceSlugs =
    [
        "dua-don-san-bay", "bua-sang-tai-phong", "giat-ui-va-may-giat",
        "goi-y-hanh-trinh", "thue-xe-dap-va-xe-may", "luu-tru-dai-ngay"
    ];

    public static async Task SeedAsync(
        HbpDbContext db,
        IImageProcessor processor,
        IMediaStorage storage,
        CancellationToken cancellationToken)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Persistence", "Seed", "Assets");
        var manifestFile = Path.Combine(root, "seed-manifest.json");
        if (!File.Exists(manifestFile))
            throw new InvalidOperationException($"Frontend seed manifest was not published: {manifestFile}");

        var manifest = JsonSerializer.Deserialize<SeedManifest>(
            await File.ReadAllTextAsync(manifestFile, cancellationToken), JsonOptions)
            ?? throw new InvalidOperationException("Frontend seed manifest is invalid.");
        var viJson = await File.ReadAllTextAsync(Path.Combine(root, "messages", "vi.json"), cancellationToken);
        var jaJson = await File.ReadAllTextAsync(Path.Combine(root, "messages", "ja.json"), cancellationToken);
        using var vi = JsonDocument.Parse(viJson);
        using var ja = JsonDocument.Parse(jaJson);

        var media = await db.MediaFiles
            .Where(x => x.OriginalFileName.StartsWith(MediaPrefix))
            .ToDictionaryAsync(x => x.OriginalFileName, cancellationToken);

        async Task<MediaFile> ImportMediaAsync(string relativePath, string? altVi, string? altJa)
        {
            var key = MediaPrefix + relativePath.Replace('\\', '-').Replace('/', '-');
            if (media.TryGetValue(key, out var existing)) return existing;

            var sourcePath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!sourcePath.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase) || !File.Exists(sourcePath))
                throw new InvalidOperationException($"Frontend seed image is missing: {relativePath}");

            await using var stream = File.OpenRead(sourcePath);
            var processed = await processor.ProcessAsync(stream, cancellationToken);
            var id = Guid.NewGuid();
            var paths = await storage.SaveAsync(id, processed, cancellationToken);
            var entity = new MediaFile
            {
                Id = id,
                OriginalFileName = key,
                StoredFileName = "original.webp",
                StoragePath = paths.Original,
                PublicUrl = paths.PublicUrl,
                MimeType = "image/webp",
                SizeBytes = processed.Original.LongLength,
                Width = processed.Width,
                Height = processed.Height,
                AltTextVi = altVi,
                AltTextJa = altJa
            };
            db.MediaFiles.Add(entity);
            media[key] = entity;
            return entity;
        }

        var viRooms = vi.RootElement.GetProperty("rooms").GetProperty("items");
        var jaRooms = ja.RootElement.GetProperty("rooms").GetProperty("items");
        var rooms = await db.RoomTypes.ToDictionaryAsync(x => x.Code, cancellationToken);
        var amenities = await db.Amenities.ToListAsync(cancellationToken);
        var roomAmenities = await db.RoomTypeAmenities.ToListAsync(cancellationToken);
        var roomMedia = await db.RoomTypeMedia.ToListAsync(cancellationToken);

        for (var roomIndex = 0; roomIndex < manifest.Rooms.Count; roomIndex++)
        {
            var source = manifest.Rooms[roomIndex];
            var roomVi = viRooms.GetProperty(source.Id);
            var roomJa = jaRooms.GetProperty(source.Id);
            if (!rooms.TryGetValue(source.Code, out var room))
            {
                room = new RoomType { Id = Guid.NewGuid(), Code = source.Code };
                db.RoomTypes.Add(room);
                rooms[source.Code] = room;
            }

            room.Slug = source.Id;
            room.NameVi = Text(roomVi, "name");
            room.NameJa = Text(roomJa, "name");
            room.ShortDescriptionVi = Text(roomVi, "summary");
            room.ShortDescriptionJa = Text(roomJa, "summary");
            room.DescriptionVi = Text(roomVi, "description");
            room.DescriptionJa = Text(roomJa, "description");
            room.PriceVnd = source.PriceVnd;
            room.PriceDisplayMode = PriceDisplayMode.SHOW_PRICE;
            room.Capacity = source.Guests;
            room.AreaSquareMeters = source.SizeM2;
            room.BedDescriptionVi = $"{Text(roomVi, "bedType")} · {Text(roomVi, "viewType")}";
            room.BedDescriptionJa = $"{Text(roomJa, "bedType")} · {Text(roomJa, "viewType")}";
            room.DisplayOrder = roomIndex;
            room.IsVisible = true;
            room.SeoTitleVi = $"{room.NameVi} | BB Homes";
            room.SeoTitleJa = $"{room.NameJa} | BB Homes";
            room.SeoDescriptionVi = room.ShortDescriptionVi;
            room.SeoDescriptionJa = room.ShortDescriptionJa;

            for (var imageIndex = 0; imageIndex < RoomImages.Length; imageIndex++)
            {
                var relativePath = $"bbhomes/{source.Id}/{RoomImages[imageIndex]}";
                var imported = await ImportMediaAsync(
                    relativePath,
                    $"{room.NameVi} - ảnh {imageIndex + 1}",
                    $"{room.NameJa} - 画像 {imageIndex + 1}");
                if (imageIndex == 0) room.FeaturedMediaId = imported.Id;
                if (roomMedia.All(x => x.RoomTypeId != room.Id || x.MediaFileId != imported.Id))
                {
                    var link = new RoomTypeMedia
                    {
                        Id = Guid.NewGuid(),
                        RoomTypeId = room.Id,
                        MediaFileId = imported.Id,
                        DisplayOrder = imageIndex
                    };
                    db.RoomTypeMedia.Add(link);
                    roomMedia.Add(link);
                }
            }

            var amenityVi = Strings(roomVi, "amenities");
            var amenityJa = Strings(roomJa, "amenities");
            for (var index = 0; index < amenityVi.Length; index++)
            {
                var amenity = amenities.FirstOrDefault(x => x.NameVi == amenityVi[index]);
                if (amenity is null)
                {
                    amenity = new Amenity
                    {
                        Id = Guid.NewGuid(),
                        NameVi = amenityVi[index],
                        NameJa = amenityJa.ElementAtOrDefault(index),
                        Icon = AmenityIcon(amenityVi[index]),
                        DisplayOrder = amenities.Count,
                        IsVisible = true
                    };
                    db.Amenities.Add(amenity);
                    amenities.Add(amenity);
                }
                else if (string.IsNullOrWhiteSpace(amenity.NameJa))
                    amenity.NameJa = amenityJa.ElementAtOrDefault(index);

                if (roomAmenities.All(x => x.RoomTypeId != room.Id || x.AmenityId != amenity.Id))
                {
                    var link = new RoomTypeAmenity
                    {
                        RoomTypeId = room.Id,
                        AmenityId = amenity.Id,
                        DisplayOrder = index
                    };
                    db.RoomTypeAmenities.Add(link);
                    roomAmenities.Add(link);
                }
            }
        }

        var viServices = vi.RootElement.GetProperty("services").GetProperty("list").GetProperty("items");
        var jaServices = ja.RootElement.GetProperty("services").GetProperty("list").GetProperty("items");
        var services = await db.Services.ToDictionaryAsync(x => x.Slug, cancellationToken);
        for (var index = 0; index < ServiceSlugs.Length; index++)
        {
            var viItem = viServices[index];
            var jaItem = jaServices[index];
            if (!services.TryGetValue(ServiceSlugs[index], out var service))
            {
                service = new Service { Id = Guid.NewGuid(), Slug = ServiceSlugs[index] };
                db.Services.Add(service);
                services[service.Slug] = service;
            }
            service.NameVi = Text(viItem, "title");
            service.NameJa = Text(jaItem, "title");
            service.ShortDescriptionVi = Text(viItem, "body");
            service.ShortDescriptionJa = Text(jaItem, "body");
            service.DescriptionVi = service.ShortDescriptionVi;
            service.DescriptionJa = service.ShortDescriptionJa;
            service.DisplayOrder = index;
            service.IsVisible = true;
        }

        var categoryDefinitions = new[]
        {
            new { Slug = "rooms", Vi = "Phòng nghỉ", Ja = "客室" },
            new { Slug = "spaces", Vi = "Ngôi nhà", Ja = "建物" },
            new { Slug = "details", Vi = "Chi tiết", Ja = "ディテール" }
        };
        var categories = await db.GalleryCategories.ToDictionaryAsync(x => x.Slug, cancellationToken);
        for (var index = 0; index < categoryDefinitions.Length; index++)
        {
            var definition = categoryDefinitions[index];
            if (!categories.TryGetValue(definition.Slug, out var category))
            {
                category = new GalleryCategory { Id = Guid.NewGuid(), Slug = definition.Slug };
                db.GalleryCategories.Add(category);
                categories[definition.Slug] = category;
            }
            category.NameVi = definition.Vi;
            category.NameJa = definition.Ja;
            category.DisplayOrder = index;
            category.IsVisible = true;
        }

        var galleryItems = await db.GalleryItems.ToListAsync(cancellationToken);
        for (var index = 0; index < manifest.Gallery.Count; index++)
        {
            var source = manifest.Gallery[index];
            var imported = await ImportMediaAsync(source.Path, null, null);
            var category = categories[source.Category];
            if (galleryItems.All(x => x.MediaFileId != imported.Id || x.GalleryCategoryId != category.Id))
            {
                var item = new GalleryItem
                {
                    Id = Guid.NewGuid(),
                    MediaFileId = imported.Id,
                    GalleryCategoryId = category.Id,
                    DisplayOrder = index,
                    IsVisible = true
                };
                db.GalleryItems.Add(item);
                galleryItems.Add(item);
            }
        }

        foreach (var source in manifest.AdditionalMedia)
            await ImportMediaAsync(source.Path, source.AltVi, source.AltJa);

        var sourceSetting = await db.SystemSettings.SingleOrDefaultAsync(
            x => x.Key == "frontend_seed_source", cancellationToken);
        var sourceValue = JsonSerializer.Serialize(new
        {
            sourceProject = @"D:\CauHinh\FE_Bbhome",
            locales = new[] { "vi", "ja" },
            rooms = manifest.Rooms.Count,
            galleryItems = manifest.Gallery.Count,
            importedMedia = media.Count
        });
        if (sourceSetting is null)
            db.SystemSettings.Add(new SystemSetting
            {
                Key = "frontend_seed_source",
                Value = sourceValue,
                Description = "Audit metadata for content imported from the original frontend"
            });
        else
            sourceSetting.Value = sourceValue;

        await UpsertSettingAsync(
            db, "frontend_content_vi", viJson,
            "Original Vietnamese UI content imported from the frontend", cancellationToken);
        await UpsertSettingAsync(
            db, "frontend_content_ja", jaJson,
            "Original Japanese UI content imported from the frontend", cancellationToken);
        await UpsertSettingAsync(
            db, "frontend_seed_manifest",
            await File.ReadAllTextAsync(manifestFile, cancellationToken),
            "Room, gallery and media mapping imported from the frontend", cancellationToken);
    }

    private static string Text(JsonElement element, string property) =>
        element.GetProperty(property).GetString()
        ?? throw new InvalidOperationException($"Required translation '{property}' is null.");

    private static string[] Strings(JsonElement element, string property) =>
        element.GetProperty(property).EnumerateArray().Select(x => x.GetString()!).ToArray();

    private static string AmenityIcon(string value)
    {
        var normalized = value.ToLowerInvariant();
        if (normalized.Contains("tv")) return "tv";
        if (normalized.Contains("điều hòa")) return "air-conditioning";
        if (normalized.Contains("bếp")) return "kitchen";
        if (normalized.Contains("máy giặt")) return "washing-machine";
        if (normalized.Contains("tủ lạnh")) return "refrigerator";
        if (normalized.Contains("ban công")) return "balcony";
        if (normalized.Contains("cửa")) return "window";
        if (normalized.Contains("bàn")) return "desk";
        return "check";
    }

    private static async Task UpsertSettingAsync(
        HbpDbContext db,
        string key,
        string value,
        string description,
        CancellationToken cancellationToken)
    {
        var setting = await db.SystemSettings.SingleOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (setting is null)
            db.SystemSettings.Add(new SystemSetting { Key = key, Value = value, Description = description });
        else
        {
            setting.Value = value;
            setting.Description = description;
        }
    }

    private sealed record SeedManifest(
        List<SeedRoom> Rooms,
        List<SeedGalleryItem> Gallery,
        List<SeedAdditionalMedia> AdditionalMedia);
    private sealed record SeedRoom(string Id, string Code, string Label, decimal SizeM2, int Guests, decimal PriceVnd);
    private sealed record SeedGalleryItem(string Path, string Category);
    private sealed record SeedAdditionalMedia(string Path, string AltVi, string AltJa);
}
