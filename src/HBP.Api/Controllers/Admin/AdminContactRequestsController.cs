using HBP.Application.Admin;
using HBP.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/contact-requests")]
public sealed class AdminContactRequestsController(IAdminContactRequestService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<AdminContactRequestListItem>> List([FromQuery] PageQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public Task<AdminContactRequestResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);
}
