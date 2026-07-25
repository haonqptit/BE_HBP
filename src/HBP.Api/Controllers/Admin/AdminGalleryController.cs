using HBP.Application.Admin;
using HBP.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Controllers.Admin;

[ApiController]
[Authorize]
[Route("api/admin/gallery")]
public sealed class AdminGalleryController(IAdminGalleryService service) : ControllerBase
{
    [HttpGet("categories")]
    public Task<PagedResult<AdminGalleryCategoryResponse>> ListCategories([FromQuery] PageQuery query, CancellationToken ct) =>
        service.ListCategoriesAsync(query, ct);

    [HttpGet("categories/{id:guid}")]
    public Task<AdminGalleryCategoryResponse> GetCategory(Guid id, CancellationToken ct) =>
        service.GetCategoryAsync(id, ct);

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(SaveGalleryCategoryRequest request, CancellationToken ct)
    {
        var result = await service.CreateCategoryAsync(request, ct);
        return Created($"/api/admin/gallery/categories/{result.Id}", result);
    }

    [HttpPut("categories/{id:guid}")]
    public Task<AdminGalleryCategoryResponse> UpdateCategory(Guid id, SaveGalleryCategoryRequest request, CancellationToken ct) =>
        service.UpdateCategoryAsync(id, request, ct);

    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct)
    {
        await service.DeleteCategoryAsync(id, ct);
        return NoContent();
    }

    [HttpGet("items")]
    public Task<PagedResult<AdminGalleryItemResponse>> ListItems([FromQuery] Guid? categoryId,
        [FromQuery] PageQuery query, CancellationToken ct) => service.ListItemsAsync(categoryId, query, ct);

    [HttpGet("items/{id:guid}")]
    public Task<AdminGalleryItemResponse> GetItem(Guid id, CancellationToken ct) => service.GetItemAsync(id, ct);

    [HttpPost("items")]
    public async Task<IActionResult> CreateItem(SaveGalleryItemRequest request, CancellationToken ct)
    {
        var result = await service.CreateItemAsync(request, ct);
        return Created($"/api/admin/gallery/items/{result.Id}", result);
    }

    [HttpPut("items/{id:guid}")]
    public Task<AdminGalleryItemResponse> UpdateItem(Guid id, SaveGalleryItemRequest request, CancellationToken ct) =>
        service.UpdateItemAsync(id, request, ct);

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id, CancellationToken ct)
    {
        await service.DeleteItemAsync(id, ct);
        return NoContent();
    }
}
