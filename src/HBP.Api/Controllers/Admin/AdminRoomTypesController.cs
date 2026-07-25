using HBP.Application.Admin;
using HBP.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/rooms")]
public sealed class AdminRoomTypesController(IAdminRoomTypeService service) : ControllerBase
{
    [HttpGet]
    public Task<PagedResult<AdminRoomTypeListItem>> List([FromQuery] PageQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    public Task<AdminRoomTypeResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost]
    public async Task<IActionResult> Create(SaveRoomTypeRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return Created($"/api/admin/rooms/{result.Id}", result);
    }

    [HttpPut("{id:guid}")]
    public Task<AdminRoomTypeResponse> Update(Guid id, SaveRoomTypeRequest request, CancellationToken ct) =>
        service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/amenities")]
    public Task<AdminRoomTypeResponse> ReplaceAmenities(Guid id, ReplaceLinksRequest request, CancellationToken ct) =>
        service.ReplaceAmenitiesAsync(id, request, ct);

    [HttpPut("{id:guid}/media")]
    public Task<AdminRoomTypeResponse> ReplaceMedia(Guid id, ReplaceLinksRequest request, CancellationToken ct) =>
        service.ReplaceMediaAsync(id, request, ct);
}
