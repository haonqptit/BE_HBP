using HBP.Application.Admin;
using HBP.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/services")]
public sealed class AdminServicesController(IAdminServiceCatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<AdminServiceResponse>> List([FromQuery] PageQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public Task<AdminServiceResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost]
    public async Task<IActionResult> Create(SaveServiceRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return Created($"/api/admin/services/{result.Id}", result);
    }

    [HttpPut("{id:guid}")]
    public Task<AdminServiceResponse> Update(Guid id, SaveServiceRequest request, CancellationToken ct) =>
        service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
