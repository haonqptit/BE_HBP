using HBP.Application.Admin;
using HBP.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/amenities")]
public sealed class AdminAmenitiesController(IAdminAmenityService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<AdminAmenityResponse>> List([FromQuery] PageQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public Task<AdminAmenityResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost]
    public async Task<IActionResult> Create(SaveAmenityRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return Created($"/api/admin/amenities/{result.Id}", result);
    }

    [HttpPut("{id:guid}")]
    public Task<AdminAmenityResponse> Update(Guid id, SaveAmenityRequest request, CancellationToken ct) =>
        service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
