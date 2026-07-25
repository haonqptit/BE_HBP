using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace HBP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class AdminSettingsTests(PostgresFixture fixture)
{
    private const string Password = "Correct-Horse-Battery-4";

    [Fact]
    public async Task SettingsCanBeUpsertedAndReadBack()
    {
        await AdminSession.CreateAdminAsync(fixture, "settings_admin", Password);
        var client = await AdminSession.SignInAsync(fixture, "settings_admin", Password);

        var updated = await client.PutAsJsonAsync("/api/admin/settings/notification_emails",
            new { value = """["ops@example.com","manager@example.com"]""", description = "Recipients" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var read = await client.GetAsync("/api/admin/settings/notification_emails");
        using var body = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
        Assert.Contains("manager@example.com", body.RootElement.GetProperty("value").GetString());

        var all = await client.GetAsync("/api/admin/settings");
        using var allBody = JsonDocument.Parse(await all.Content.ReadAsStringAsync());
        Assert.Contains(allBody.RootElement.EnumerateArray(),
            x => x.GetProperty("key").GetString() == "notification_emails");
    }

    [Fact]
    public async Task SecretLookingKeysAreRefused()
    {
        await AdminSession.CreateAdminAsync(fixture, "secrets_admin", Password);
        var client = await AdminSession.SignInAsync(fixture, "secrets_admin", Password);

        var response = await client.PutAsJsonAsync("/api/admin/settings/smtp_password", new { value = "hunter2" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownSettingReturnsNotFound()
    {
        await AdminSession.CreateAdminAsync(fixture, "missing_setting_admin", Password);
        var client = await AdminSession.SignInAsync(fixture, "missing_setting_admin", Password);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/admin/settings/does_not_exist")).StatusCode);
    }
}
