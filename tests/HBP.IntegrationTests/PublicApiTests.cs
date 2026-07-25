using System.Net;
using System.Text.Json;
using Npgsql;

namespace HBP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class PublicApiTests(PostgresFixture fixture)
{
    [Fact]
    public async Task SiteMetadata_IsPublicAndLocalized()
    {
        await using (var connection = new NpgsqlConnection(fixture.Container.GetConnectionString()))
        {
            await connection.OpenAsync();
            const string value = """
                {"name":"BB Homes","addressVi":"Hà Nội","addressJa":"ハノイ","phone":"0123456789",
                 "email":"hello@example.com","checkInVi":"14:00","checkInJa":"14時",
                 "checkOutVi":"12:00","checkOutJa":"12時","receptionVi":"24/7","receptionJa":"24時間"}
            """;
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO system_settings(key,value,description)
                VALUES ('site_metadata',@value,'Public site metadata')
                ON CONFLICT(key) DO UPDATE SET value=EXCLUDED.value
                """, connection);
            command.Parameters.AddWithValue("value", value);
            await command.ExecuteNonQueryAsync();
        }

        var response = await fixture.Factory.CreateClient().GetAsync("/api/site-metadata?lang=ja");
        var json = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, json);
        using var document = JsonDocument.Parse(json);
        Assert.Equal("ハノイ", document.RootElement.GetProperty("address").GetString());
        Assert.Equal("hello@example.com", document.RootElement.GetProperty("email").GetString());
        Assert.NotNull(response.Headers.ETag);
    }

    [Fact]
    public async Task Rooms_RespectVisibilityLanguagePricingAndEtag()
    {
        await using (var connection = new NpgsqlConnection(fixture.Container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await new NpgsqlCommand("DELETE FROM room_types", connection).ExecuteNonQueryAsync();
            var sql = """
                INSERT INTO room_types(code,slug,name_vi,name_ja,price_vnd,price_display_mode,capacity,display_order,is_visible)
                VALUES
                ('CONTACT','contact-room','Phòng liên hệ',NULL,1000000,'CONTACT',2,1,true),
                ('PRICED','priced-room','Phòng có giá','料金の部屋',2000000,'SHOW_PRICE',2,2,true),
                ('HIDDEN','hidden-room','Phòng ẩn',NULL,3000000,'SHOW_PRICE',2,3,false)
                """;
            await new NpgsqlCommand(sql, connection).ExecuteNonQueryAsync();
        }

        var client = fixture.Factory.CreateClient();
        var response = await client.GetAsync("/api/rooms?lang=ja");
        var json = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, json);
        using var document = JsonDocument.Parse(json);
        var rooms = document.RootElement;
        Assert.Equal(2, rooms.GetArrayLength());
        Assert.Equal("Phòng liên hệ", rooms[0].GetProperty("name").GetString());
        Assert.False(rooms[0].TryGetProperty("priceVnd", out _));
        Assert.Equal("料金の部屋", rooms[1].GetProperty("name").GetString());
        Assert.Equal(2_000_000m, rooms[1].GetProperty("priceVnd").GetDecimal());
        Assert.NotNull(response.Headers.ETag);

        using var secondRequest = new HttpRequestMessage(HttpMethod.Get, "/api/rooms?lang=ja");
        secondRequest.Headers.IfNoneMatch.Add(response.Headers.ETag!);
        var second = await client.SendAsync(secondRequest);
        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
        Assert.Empty(await second.Content.ReadAsByteArrayAsync());

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/rooms/hidden-room")).StatusCode);
    }
}
