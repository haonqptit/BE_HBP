using HBP.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace HBP.Infrastructure.Admin;

internal static class AdminPaging
{
    /// <summary>
    /// Counts, pages and projects an already-sorted query. Materialisation happens before mapping so
    /// the map delegate can use helpers EF cannot translate.
    /// </summary>
    public static async Task<PagedResult<TOut>> ToPagedResultAsync<TIn, TOut>(IQueryable<TIn> query,
        PageQuery request, Func<TIn, TOut> map, CancellationToken cancellationToken)
    {
        var page = request.NormalizedPage;
        var pageSize = request.NormalizedPageSize;
        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<TOut>(rows.Select(map).ToList(), page, pageSize, total);
    }
}
