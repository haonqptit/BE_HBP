using HBP.Application.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/media")]
public sealed class MediaController(IMediaService service) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? altTextVi,
        [FromForm] string? altTextJa, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await service.UploadAsync(stream, file.FileName, file.ContentType, file.Length, altTextVi, altTextJa, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet]
    public Task<HBP.Application.Common.PagedResult<MediaResponse>> List([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        service.ListAsync(page, pageSize, cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<MediaResponse> Get(Guid id, CancellationToken cancellationToken) => service.GetAsync(id, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
