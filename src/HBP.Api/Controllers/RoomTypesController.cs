using HBP.Application.Public;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Controllers;

[ApiController, Route("api/rooms")]
public sealed class RoomTypesController(IPublicRoomTypeQueryService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<RoomTypeListItemResponse>> List(CancellationToken ct) => service.ListAsync(ct);
    [HttpGet("{slug}")] public Task<RoomTypeDetailResponse> Get(string slug, CancellationToken ct) => service.GetAsync(slug, ct);
}
