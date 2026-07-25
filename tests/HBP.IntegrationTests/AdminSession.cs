using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HBP.Infrastructure.Auth;
using Npgsql;

namespace HBP.IntegrationTests;

/// <summary>
/// Creates an admin_users row and returns an HttpClient that is logged in and already carries the
/// CSRF header required by <c>AdminCsrfMiddleware</c> for every mutating admin request.
/// </summary>
internal static class AdminSession
{
    public static async Task<string> CreateAdminAsync(PostgresFixture fixture, string username, string password)
    {
        var hash = new PasswordHasherAdapter().Hash(password);
        await using var connection = new NpgsqlConnection(fixture.Container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            INSERT INTO admin_users(username, email, password_hash, is_active)
            VALUES (@username, @email, @hash, true)
            """, connection);
        command.Parameters.AddWithValue("username", username);
        command.Parameters.AddWithValue("email", $"{username}@example.com");
        command.Parameters.AddWithValue("hash", hash);
        await command.ExecuteNonQueryAsync();
        return username;
    }

    public static async Task<HttpClient> SignInAsync(PostgresFixture fixture, string username, string password)
    {
        var client = fixture.Factory.CreateClient();
        var token = await AttachCsrfAsync(client);
        var response = await client.PostAsJsonAsync("/api/admin/auth/login", new { username, password });
        if (response.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"Admin login failed with {(int)response.StatusCode}.");
        client.DefaultRequestHeaders.Remove("X-HBP-CSRF");
        client.DefaultRequestHeaders.Add("X-HBP-CSRF", token);
        return client;
    }

    private static async Task<string> AttachCsrfAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/admin/auth/csrf");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("token").GetString()!;
    }
}
