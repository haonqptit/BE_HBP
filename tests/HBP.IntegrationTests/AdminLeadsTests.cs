using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

namespace HBP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class AdminLeadsTests(PostgresFixture fixture)
{
    private const string Password = "Correct-Horse-Battery-3";

    [Fact]
    public async Task BookingSearchMatchesNameEmailAndPhoneAndDetailCarriesDeliveries()
    {
        await AdminSession.CreateAdminAsync(fixture, "leads_admin", Password);
        var client = await AdminSession.SignInAsync(fixture, "leads_admin", Password);

        await Execute("""
            INSERT INTO system_settings(key,value) VALUES ('notification_emails','["ops@example.com"]')
            ON CONFLICT(key) DO UPDATE SET value=EXCLUDED.value
            """);
        var guest = fixture.Factory.CreateClient();
        guest.DefaultRequestHeaders.Add("X-Forwarded-For", "10.20.30.44");
        var submission = await guest.PostAsJsonAsync("/api/booking-requests", new
        {
            fullName = "Trinh Thi Bich Lien", email = "bichlien@example.com", phoneNumber = "0987654321",
            adults = 2, languageCode = "vi"
        });
        Assert.Equal(HttpStatusCode.Created, submission.StatusCode);

        foreach (var term in new[] { "bich lien", "BICHLIEN@example", "987654" })
        {
            var response = await client.GetAsync($"/api/admin/booking-requests?search={Uri.EscapeDataString(term)}");
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(body.RootElement.GetProperty("totalCount").GetInt64() >= 1, $"no match for '{term}'");
        }

        var listed = await client.GetAsync("/api/admin/booking-requests?search=bichlien@example.com");
        using var listedBody = JsonDocument.Parse(await listed.Content.ReadAsStringAsync());
        var id = listedBody.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        var detail = await client.GetAsync($"/api/admin/booking-requests/{id}");
        using var detailBody = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        // One admin notification plus one guest confirmation.
        Assert.Equal(2, detailBody.RootElement.GetProperty("emailDeliveries").GetArrayLength());
        Assert.Equal("RECEIVED", detailBody.RootElement.GetProperty("status").GetString());

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/admin/booking-requests/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task PagingClampsPageSizeToOneHundred()
    {
        await AdminSession.CreateAdminAsync(fixture, "paging_admin", Password);
        var client = await AdminSession.SignInAsync(fixture, "paging_admin", Password);

        var response = await client.GetAsync("/api/admin/contact-requests?page=0&pageSize=5000");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, body.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(100, body.RootElement.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task DashboardCountsNewLeadsAndFailedDeliveries()
    {
        await AdminSession.CreateAdminAsync(fixture, "dashboard_admin", Password);
        var client = await AdminSession.SignInAsync(fixture, "dashboard_admin", Password);

        var before = await ReadDashboardAsync(client);
        await Execute("""
            INSERT INTO contact_requests(reference_code, full_name, email, phone_number, subject, message, language_code)
            VALUES ('CT-DASH1-TEST01', 'Dashboard Probe', 'dashboard@example.com', '0900000000', 'Hi', 'Body', 'vi')
            """);
        await Execute("""
            INSERT INTO email_deliveries(related_entity_type, related_entity_id, email_type, recipient, language_code, status, attempt_count)
            VALUES ('ContactRequest', gen_random_uuid(), 'CONTACT_ADMIN_NOTIFICATION', 'ops@example.com', 'vi', 'FAILED', 6)
            """);

        var after = await ReadDashboardAsync(client);
        Assert.Equal(before.contacts7 + 1, after.contacts7);
        Assert.Equal(before.contactsTotal + 1, after.contactsTotal);
        Assert.Equal(before.failed + 1, after.failed);
    }

    private static async Task<(long contacts7, long contactsTotal, long failed)> ReadDashboardAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/admin/dashboard");
        var json = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, json);
        using var body = JsonDocument.Parse(json);
        var contacts = body.RootElement.GetProperty("contactRequests");
        return (contacts.GetProperty("last7Days").GetInt64(), contacts.GetProperty("total").GetInt64(),
            body.RootElement.GetProperty("failedEmailDeliveries").GetInt64());
    }

    private async Task Execute(string sql)
    {
        await using var connection = new NpgsqlConnection(fixture.Container.GetConnectionString());
        await connection.OpenAsync();
        await new NpgsqlCommand(sql, connection).ExecuteNonQueryAsync();
    }
}
