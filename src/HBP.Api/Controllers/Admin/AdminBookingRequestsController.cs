using HBP.Application.Admin;
using HBP.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/booking-requests")]
public sealed class AdminBookingRequestsController(IAdminBookingRequestService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<AdminBookingRequestListItem>> List([FromQuery] PageQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public Task<AdminBookingRequestResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);
}
