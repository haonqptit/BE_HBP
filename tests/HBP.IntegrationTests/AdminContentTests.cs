using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace HBP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class AdminContentTests(PostgresFixture fixture)
{
    private const string Password = "Correct-Horse-Battery-2";

    [Fact]
    public async Task RoomLifecycleCoversUploadAssignmentDuplicatesAndInUseDeletion()
    {
        await AdminSession.CreateAdminAsync(fixture, "content_admin", Password);
        var client = await AdminSession.SignInAsync(fixture, "content_admin", Password);

        var media = await UploadAsync(client, 1300, 900);
        Assert.Equal(1300, media.GetProperty("width").GetInt32());
        Assert.EndsWith("/original.webp", media.GetProperty("publicUrl").GetString());
        Assert.EndsWith("/medium.webp", media.GetProperty("mediumUrl").GetString());
        Assert.EndsWith("/thumbnail.webp", media.GetProperty("thumbnailUrl").GetString());
        var mediaId = media.GetProperty("id").GetGuid();

        var created = await client.PostAsJsonAsync("/api/admin/rooms", RoomPayload("ADM1", "admin-suite", mediaId));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var room = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var roomId = room.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(mediaId, room.RootElement.GetProperty("featuredMedia").GetProperty("id").GetGuid());

        var duplicate = await client.PostAsJsonAsync("/api/admin/rooms", RoomPayload("ADM2", "admin-suite", null));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var amenity = await client.PostAsJsonAsync("/api/admin/amenities",
            new { nameVi = "Bồn tắm", nameJa = "バスタブ", displayOrder = 0, isVisible = true });
        Assert.Equal(HttpStatusCode.Created, amenity.StatusCode);
        using var amenityBody = JsonDocument.Parse(await amenity.Content.ReadAsStringAsync());
        var amenityId = amenityBody.RootElement.GetProperty("id").GetGuid();

        var linked = await client.PutAsJsonAsync($"/api/admin/rooms/{roomId}/amenities",
            new { items = new[] { new { id = amenityId, displayOrder = 0 } } });
        Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
        using var linkedBody = JsonDocument.Parse(await linked.Content.ReadAsStringAsync());
        Assert.Equal(amenityId, linkedBody.RootElement.GetProperty("amenities")[0].GetProperty("amenityId").GetGuid());

        // Replacing twice must be idempotent — the second call re-inserts the same media row.
        for (var i = 0; i < 2; i++)
        {
            var withMedia = await client.PutAsJsonAsync($"/api/admin/rooms/{roomId}/media",
                new { items = new[] { new { id = mediaId, displayOrder = 0 } } });
            Assert.Equal(HttpStatusCode.OK, withMedia.StatusCode);
        }

        var blocked = await client.DeleteAsync($"/api/admin/media/{mediaId}");
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        using var problem = JsonDocument.Parse(await blocked.Content.ReadAsStringAsync());
        var references = problem.RootElement.GetProperty("references").EnumerateArray()
            .Select(x => x.GetString()).ToList();
        Assert.Contains("room_types.featured_media_id", references);
        Assert.Contains("room_type_media", references);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/admin/rooms/{roomId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/admin/media/{mediaId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/admin/rooms/{roomId}")).StatusCode);
    }

    [Fact]
    public async Task FeaturedImageBelowMinimumSizeIsRejected()
    {
        await AdminSession.CreateAdminAsync(fixture, "small_image_admin", Password);
        var client = await AdminSession.SignInAsync(fixture, "small_image_admin", Password);

        var media = await UploadAsync(client, 400, 300);
        var mediaId = media.GetProperty("id").GetGuid();

        var response = await client.PostAsJsonAsync("/api/admin/rooms", RoomPayload("SMALL", "small-featured", mediaId));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("featuredMediaId", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task NonImageUploadIsRejected()
    {
        await AdminSession.CreateAdminAsync(fixture, "bad_upload_admin", Password);
        var client = await AdminSession.SignInAsync(fixture, "bad_upload_admin", Password);

        using var form = new MultipartFormDataContent();
        var content = new ByteArrayContent("not really a png"u8.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(content, "file", "fake.png");
        var response = await client.PostAsync("/api/admin/media", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GalleryCategoryAndItemRoundTrip()
    {
        await AdminSession.CreateAdminAsync(fixture, "gallery_admin", Password);
        var client = await AdminSession.SignInAsync(fixture, "gallery_admin", Password);

        var category = await client.PostAsJsonAsync("/api/admin/gallery/categories",
            new { slug = "su-kien", nameVi = "Sự kiện", nameJa = "イベント", displayOrder = 0, isVisible = true });
        Assert.Equal(HttpStatusCode.Created, category.StatusCode);
        using var categoryBody = JsonDocument.Parse(await category.Content.ReadAsStringAsync());
        var categoryId = categoryBody.RootElement.GetProperty("id").GetGuid();

        var media = await UploadAsync(client, 900, 600);
        var item = await client.PostAsJsonAsync("/api/admin/gallery/items", new
        {
            galleryCategoryId = categoryId, mediaFileId = media.GetProperty("id").GetGuid(),
            captionVi = "Tiệc cưới", displayOrder = 0, isVisible = true
        });
        Assert.Equal(HttpStatusCode.Created, item.StatusCode);

        var listed = await client.GetAsync($"/api/admin/gallery/items?categoryId={categoryId}");
        using var listedBody = JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
        Assert.Equal(1, listedBody.RootElement.GetProperty("totalCount").GetInt64());
        Assert.Equal("su-kien", listedBody.RootElement.GetProperty("items")[0].GetProperty("galleryCategorySlug").GetString());

        var unknownCategory = await client.PostAsJsonAsync("/api/admin/gallery/items", new
        {
            galleryCategoryId = Guid.NewGuid(), mediaFileId = media.GetProperty("id").GetGuid(),
            displayOrder = 0, isVisible = true
        });
        Assert.Equal(HttpStatusCode.BadRequest, unknownCategory.StatusCode);
    }

    private static object RoomPayload(string code, string slug, Guid? featuredMediaId) => new
    {
        code, slug, nameVi = "Phòng quản trị", nameJa = "管理室", priceDisplayMode = "CONTACT",
        capacity = 2, displayOrder = 0, isVisible = true, featuredMediaId
    };

    private static async Task<JsonElement> UploadAsync(HttpClient client, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var buffer = new MemoryStream();
        await image.SaveAsync(buffer, new PngEncoder());

        using var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(buffer.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(content, "file", $"sample-{width}x{height}.png");
        var response = await client.PostAsync("/api/admin/media", form);
        var json = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, json);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
