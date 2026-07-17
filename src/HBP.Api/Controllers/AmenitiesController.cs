using HBP.Application.Public;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Controllers;

[ApiController, Route("api/amenities")]
public sealed class AmenitiesController(IPublicAmenityQueryService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<AmenityResponse>> List(CancellationToken ct) => service.ListAsync(ct);
}
