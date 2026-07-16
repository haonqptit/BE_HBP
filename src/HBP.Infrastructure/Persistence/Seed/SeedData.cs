using System.Text.Json;
using HBP.Application.Abstractions;
using HBP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Persistence.Seed;

public static class SeedData
{
    public static async Task InitializeAsync(HbpDbContext db, IPasswordHasher hasher, CancellationToken cancellationToken = default)
    {
        var username = Environment.GetEnvironmentVariable("HBP_SEED_ADMIN_USERNAME");
        var email = Environment.GetEnvironmentVariable("HBP_SEED_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("HBP_SEED_ADMIN_PASSWORD");

        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password)
            && !await db.AdminUsers.AnyAsync(cancellationToken))
        {
            db.AdminUsers.Add(new AdminUser
            {
                Username = username.Trim(), Email = email.Trim(), PasswordHash = hasher.Hash(password), IsActive = true
            });
        }

        if (!await db.SystemSettings.AnyAsync(x => x.Key == "notification_emails", cancellationToken))
        {
            db.SystemSettings.Add(new SystemSetting
            {
                Key = "notification_emails",
                Value = JsonSerializer.Serialize(Array.Empty<string>()),
                Description = "Recipients for booking/contact notifications"
            });
        }

        if (!await db.SystemSettings.AnyAsync(x => x.Key == "site_metadata", cancellationToken))
        {
            db.SystemSettings.Add(new SystemSetting
            {
                Key = "site_metadata", Value = "{}", Description = "Public site metadata"
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
