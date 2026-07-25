using HBP.Application.Admin;
using HBP.Application.Common;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Admin;

public sealed class AdminBookingRequestService(HbpDbContext db) : IAdminBookingRequestService
{
    public async Task<PagedResult<AdminBookingRequestListItem>> ListAsync(PageQuery query, CancellationToken cancellationToken)
    {
        var requests = db.BookingRequests.AsNoTracking().Include(x => x.RoomType).AsQueryable();
        var search = query.TrimmedSearch;
        // ILIKE '%term%' on these three columns is what the GIN pg_trgm indexes were created for.
        if (search is not null)
            requests = requests.Where(x => EF.Functions.ILike(x.FullName, $"%{search}%")
                || EF.Functions.ILike(x.Email, $"%{search}%")
                || EF.Functions.ILike(x.PhoneNumber, $"%{search}%")
                || EF.Functions.ILike(x.ReferenceCode, $"%{search}%"));
        requests = query.NormalizedSort switch
        {
            "created_at" => requests.OrderBy(x => x.CreatedAt),
            "full_name" => requests.OrderBy(x => x.FullName),
            "full_name_desc" => requests.OrderByDescending(x => x.FullName),
            _ => requests.OrderByDescending(x => x.CreatedAt)
        };
        return await AdminPaging.ToPagedResultAsync(requests, query, x => new AdminBookingRequestListItem(x.Id,
            x.ReferenceCode, x.FullName, x.Email, x.PhoneNumber, x.RoomType?.NameVi, x.CheckInDate, x.CheckOutDate,
            x.Adults, x.Children, x.NumberOfRooms, x.LanguageCode, x.Status, x.CreatedAt), cancellationToken);
    }

    public async Task<AdminBookingRequestResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var x = await db.BookingRequests.AsNoTracking().Include(b => b.RoomType)
            .SingleOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException("Booking request not found.");
        var deliveries = await AdminMapping.LoadDeliveriesAsync(db, "BookingRequest", id, cancellationToken);
        return new AdminBookingRequestResponse(x.Id, x.ReferenceCode, x.FullName, x.Email, x.PhoneNumber,
            x.RoomTypeId, x.RoomType?.NameVi, x.RoomType?.Slug, x.CheckInDate, x.CheckOutDate, x.Adults,
            x.Children, x.NumberOfRooms, x.CustomerMessage, x.LanguageCode, x.Status, x.CreatedAt, deliveries);
    }
}
