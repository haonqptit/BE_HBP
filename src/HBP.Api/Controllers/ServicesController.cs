using HBP.Application.Public;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Controllers;

[ApiController, Route("api/services")]
public sealed class ServicesController(IPublicServiceQueryService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<ServiceListItemResponse>> List(CancellationToken ct) => service.ListAsync(ct);
    [HttpGet("{slug}")] public Task<ServiceDetailResponse> Get(string slug, CancellationToken ct) => service.GetAsync(slug, ct);
}
