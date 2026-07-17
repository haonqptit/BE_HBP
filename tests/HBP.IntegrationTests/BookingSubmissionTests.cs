using System.Net;
using System.Net.Http.Json;
using Npgsql;

namespace HBP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class BookingSubmissionTests(PostgresFixture fixture)
{
    [Fact]
    public async Task BookingPersistsRequestAndQueuesAdminAndGuestEmails()
    {
        await Execute("INSERT INTO system_settings(key,value) VALUES ('notification_emails','[\"ops1@example.com\",\"ops2@example.com\"]') ON CONFLICT(key) DO UPDATE SET value=EXCLUDED.value");
        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "10.20.30.41");
        var response = await client.PostAsJsonAsync("/api/booking-requests", new
        {
            fullName = "Nguyen Van A", email = "  GUEST@EXAMPLE.COM ", phoneNumber = "+84 901 234 567",
            adults = 2, children = 0, numberOfRooms = 1, languageCode = "vi", website = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Matches(@"^BK-\d{6}-[0-9A-HJKMNP-TV-Z]{6}$", payload!["referenceCode"]);
        Assert.Equal(1L, await Scalar("SELECT count(*) FROM booking_requests WHERE email='guest@example.com'"));
        Assert.Equal(3L, await Scalar("SELECT count(*) FROM email_deliveries e JOIN booking_requests b ON e.related_entity_id=b.id WHERE b.reference_code=@code", payload["referenceCode"]));
    }

    [Fact]
    public async Task HoneypotDoesNotPersistAndSixthRequestIsRateLimited()
    {
        var before = await Scalar("SELECT count(*) FROM contact_requests");
        var bot = fixture.Factory.CreateClient();
        bot.DefaultRequestHeaders.Add("X-Forwarded-For", "10.20.30.42");
        var body = new { fullName="Bot", email="bot@example.com", phoneNumber="123456789", subject="Hi", message="Test", languageCode="vi", website="spam" };
        Assert.Equal(HttpStatusCode.Created, (await bot.PostAsJsonAsync("/api/contact-requests", body)).StatusCode);
        Assert.Equal(before, await Scalar("SELECT count(*) FROM contact_requests"));

        var client = fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "10.20.30.43");
        HttpResponseMessage? last = null;
        for (var i = 0; i < 6; i++) last = await client.PostAsJsonAsync("/api/contact-requests", body with { website = "spam" + i });
        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        Assert.Equal("60", last.Headers.GetValues("Retry-After").Single());
    }

    private async Task Execute(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.Container.GetConnectionString()); await connection.OpenAsync();
        await new NpgsqlCommand(sql, connection).ExecuteNonQueryAsync();
    }

    private async Task<long> Scalar(string sql, string? code = null)
    {
        await using var connection = new NpgsqlConnection(fixture.Container.GetConnectionString()); await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection); if (code is not null) command.Parameters.AddWithValue("code", code);
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
