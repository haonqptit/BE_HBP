using HBP.Application.Public;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Controllers;

[ApiController, Route("api/gallery")]
public sealed class GalleryController(IPublicGalleryQueryService service) : ControllerBase
{
    [HttpGet] public Task<IReadOnlyList<GalleryCategoryResponse>> List([FromQuery] string? category, CancellationToken ct) => service.ListAsync(category, ct);
}
