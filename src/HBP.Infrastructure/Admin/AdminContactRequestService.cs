using HBP.Application.Admin;
using HBP.Application.Common;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Admin;

public sealed class AdminContactRequestService(HbpDbContext db) : IAdminContactRequestService
{
    public async Task<PagedResult<AdminContactRequestListItem>> ListAsync(PageQuery query, CancellationToken cancellationToken)
    {
        var requests = db.ContactRequests.AsNoTracking().AsQueryable();
        var search = query.TrimmedSearch;
        // contact_requests carries GIN pg_trgm indexes on full_name and email only.
        if (search is not null)
            requests = requests.Where(x => EF.Functions.ILike(x.FullName, $"%{search}%")
                || EF.Functions.ILike(x.Email, $"%{search}%")
                || EF.Functions.ILike(x.ReferenceCode, $"%{search}%"));
        requests = query.NormalizedSort switch
        {
            "created_at" => requests.OrderBy(x => x.CreatedAt),
            "full_name" => requests.OrderBy(x => x.FullName),
            "full_name_desc" => requests.OrderByDescending(x => x.FullName),
            _ => requests.OrderByDescending(x => x.CreatedAt)
        };
        return await AdminPaging.ToPagedResultAsync(requests, query, x => new AdminContactRequestListItem(x.Id,
            x.ReferenceCode, x.FullName, x.Email, x.PhoneNumber, x.Subject, x.LanguageCode, x.CreatedAt), cancellationToken);
    }

    public async Task<AdminContactRequestResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var x = await db.ContactRequests.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Contact request not found.");
        var deliveries = await AdminMapping.LoadDeliveriesAsync(db, "ContactRequest", id, cancellationToken);
        return new AdminContactRequestResponse(x.Id, x.ReferenceCode, x.FullName, x.Email, x.PhoneNumber,
            x.Subject, x.Message, x.LanguageCode, x.CreatedAt, deliveries);
    }
}
