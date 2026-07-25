namespace HBP.Application.Admin;

public sealed record AdminDashboardCounts(long Last7Days, long Last30Days, long Total);

public sealed record AdminDashboardResponse(AdminDashboardCounts BookingRequests,
    AdminDashboardCounts ContactRequests, long FailedEmailDeliveries, long PendingEmailDeliveries,
    DateTime GeneratedAt);

public interface IAdminDashboardService
{
    Task<AdminDashboardResponse> GetAsync(CancellationToken cancellationToken);
}
