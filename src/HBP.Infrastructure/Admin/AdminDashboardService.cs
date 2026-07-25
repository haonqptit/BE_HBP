using HBP.Application.Abstractions;
using HBP.Application.Admin;
using HBP.Domain.Enums;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Admin;

public sealed class AdminDashboardService(HbpDbContext db, IClock clock) : IAdminDashboardService
{
    public async Task<AdminDashboardResponse> GetAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow.UtcDateTime;
        var last7 = now.AddDays(-7);
        var last30 = now.AddDays(-30);

        var bookings = new AdminDashboardCounts(
            await db.BookingRequests.CountAsync(x => x.CreatedAt >= last7, cancellationToken),
            await db.BookingRequests.CountAsync(x => x.CreatedAt >= last30, cancellationToken),
            await db.BookingRequests.LongCountAsync(cancellationToken));
        var contacts = new AdminDashboardCounts(
            await db.ContactRequests.CountAsync(x => x.CreatedAt >= last7, cancellationToken),
            await db.ContactRequests.CountAsync(x => x.CreatedAt >= last30, cancellationToken),
            await db.ContactRequests.LongCountAsync(cancellationToken));
        // FR-DASH-001: surface deliveries that exhausted their retries so the admin can act.
        var failed = await db.EmailDeliveries.LongCountAsync(x => x.Status == EmailStatus.FAILED, cancellationToken);
        var pending = await db.EmailDeliveries.LongCountAsync(
            x => x.Status == EmailStatus.PENDING || x.Status == EmailStatus.RETRYING, cancellationToken);

        return new AdminDashboardResponse(bookings, contacts, failed, pending, now);
    }
}
