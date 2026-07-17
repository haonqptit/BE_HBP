using HBP.Application.Abstractions;
using HBP.Application.Auth;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Auth;

public sealed class AuthService(HbpDbContext db, IPasswordHasher hasher, IClock clock) : IAuthService
{
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);
    private static string? _dummyHash;

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalized = request.Username.Trim().ToLowerInvariant();
        var user = await db.AdminUsers.SingleOrDefaultAsync(
            x => x.Username.ToLower() == normalized || x.Email.ToLower() == normalized,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            _dummyHash ??= hasher.Hash("hbp-timing-equalizer");
            hasher.Verify(_dummyHash, request.Password);
            return new LoginResult(false, false, null);
        }

        var now = clock.UtcNow.UtcDateTime;
        if (user.LockedUntil is not null && user.LockedUntil > now)
            return new LoginResult(false, true, null);

        if (!hasher.Verify(user.PasswordHash, request.Password))
        {
            if (user.FirstFailedAt is null || now - user.FirstFailedAt.Value > FailureWindow)
            {
                user.FirstFailedAt = now;
                user.FailedCount = 1;
            }
            else
            {
                user.FailedCount++;
            }

            if (user.FailedCount >= 5)
                user.LockedUntil = now.Add(LockDuration);

            await db.SaveChangesAsync(cancellationToken);
            return new LoginResult(false, user.LockedUntil > now, null);
        }

        user.FailedCount = 0;
        user.FirstFailedAt = null;
        user.LockedUntil = null;
        user.LastLoginAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return new LoginResult(true, false, Map(user));
    }

    public async Task<AdminUserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await db.AdminUsers.AsNoTracking().Where(x => x.Id == id && x.IsActive)
            .Select(x => new AdminUserResponse(x.Id, x.Username, x.Email))
            .SingleOrDefaultAsync(cancellationToken);

    private static AdminUserResponse Map(Domain.Entities.AdminUser user) =>
        new(user.Id, user.Username, user.Email);
}
