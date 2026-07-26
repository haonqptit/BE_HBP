using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace HBP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class AdminAuthTests(PostgresFixture fixture)
{
    private const string Password = "Correct-Horse-Battery-1";

    [Fact]
    public async Task LoginIssuesCookieAndMeReturnsTheAdmin()
    {
        await AdminSession.CreateAdminAsync(fixture, "auth_admin", Password);
        var client = await AdminSession.SignInAsync(fixture, "auth_admin", Password);

        var me = await client.GetAsync("/api/admin/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        using var document = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
        Assert.Equal("auth_admin", document.RootElement.GetProperty("username").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/admin/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/auth/me")).StatusCode);
    }

    [Fact]
    public async Task AdminEndpointsRejectAnonymousCallers()
    {
        var client = fixture.Factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/rooms")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/booking-requests")).StatusCode);
    }

    [Fact]
    public async Task MutationWithoutCsrfHeaderIsRejected()
    {
        await AdminSession.CreateAdminAsync(fixture, "csrf_admin", Password);
        var client = await AdminSession.SignInAsync(fixture, "csrf_admin", Password);
        client.DefaultRequestHeaders.Remove("X-HBP-CSRF");

        var response = await client.PostAsJsonAsync("/api/admin/amenities",
            new { nameVi = "Hồ bơi", displayOrder = 0, isVisible = true });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FiveFailedAttemptsLockTheAccountEvenWithTheRightPassword()
    {
        await AdminSession.CreateAdminAsync(fixture, "lockout_admin", Password);
        var client = fixture.Factory.CreateClient();

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var wrong = await client.PostAsJsonAsync("/api/admin/auth/login",
                new { username = "lockout_admin", password = "wrong-password" });
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }

        // The fifth failure trips the 15-minute lock, which then also rejects valid credentials.
        var fifth = await client.PostAsJsonAsync("/api/admin/auth/login",
            new { username = "lockout_admin", password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Locked, fifth.StatusCode);

        var correct = await client.PostAsJsonAsync("/api/admin/auth/login",
            new { username = "lockout_admin", password = Password });
        Assert.Equal(HttpStatusCode.Locked, correct.StatusCode);
    }

    [Fact]
    public async Task ChangePasswordRequiresCurrentPasswordAndInvalidatesTheSession()
    {
        const string username = "password_admin";
        const string newPassword = "A-New-Admin-Password-2026!";
        await AdminSession.CreateAdminAsync(fixture, username, Password);
        var client = await AdminSession.SignInAsync(fixture, username, Password);

        var incorrect = await client.PostAsJsonAsync("/api/admin/auth/change-password",
            new { currentPassword = "incorrect-password", newPassword, confirmPassword = newPassword });
        Assert.Equal(HttpStatusCode.BadRequest, incorrect.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/admin/auth/me")).StatusCode);

        var changed = await client.PostAsJsonAsync("/api/admin/auth/change-password",
            new { currentPassword = Password, newPassword, confirmPassword = newPassword });
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/admin/auth/me")).StatusCode);

        var oldPassword = await fixture.Factory.CreateClient().PostAsJsonAsync("/api/admin/auth/login",
            new { username, password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
        await AdminSession.SignInAsync(fixture, username, newPassword);
    }
}
