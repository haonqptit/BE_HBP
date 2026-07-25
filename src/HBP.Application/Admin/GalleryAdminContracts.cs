using FluentValidation;
using HBP.Application.Common;

namespace HBP.Application.Admin;

public sealed record AdminGalleryCategoryResponse(Guid Id, string Slug, string NameVi, string? NameJa,
    int DisplayOrder, bool IsVisible, int ItemCount, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record SaveGalleryCategoryRequest(string Slug, string NameVi, string? NameJa,
    int DisplayOrder, bool IsVisible);

public sealed record AdminGalleryItemResponse(Guid Id, Guid GalleryCategoryId, string GalleryCategorySlug,
    Guid MediaFileId, AdminMediaSummary Media, string? CaptionVi, string? CaptionJa,
    int DisplayOrder, bool IsVisible, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record SaveGalleryItemRequest(Guid GalleryCategoryId, Guid MediaFileId,
    string? CaptionVi, string? CaptionJa, int DisplayOrder, bool IsVisible);

public sealed class SaveGalleryCategoryRequestValidator : AbstractValidator<SaveGalleryCategoryRequest>
{
    public SaveGalleryCategoryRequestValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(150).Matches(AdminSlug.Pattern).WithMessage(AdminSlug.Message);
        RuleFor(x => x.NameVi).NotEmpty().MaximumLength(150);
        RuleFor(x => x.NameJa).MaximumLength(150);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class SaveGalleryItemRequestValidator : AbstractValidator<SaveGalleryItemRequest>
{
    public SaveGalleryItemRequestValidator()
    {
        RuleFor(x => x.GalleryCategoryId).NotEmpty();
        RuleFor(x => x.MediaFileId).NotEmpty();
        RuleFor(x => x.CaptionVi).MaximumLength(255);
        RuleFor(x => x.CaptionJa).MaximumLength(255);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public interface IAdminGalleryService
{
    Task<PagedResult<AdminGalleryCategoryResponse>> ListCategoriesAsync(PageQuery query, CancellationToken cancellationToken);
    Task<AdminGalleryCategoryResponse> GetCategoryAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminGalleryCategoryResponse> CreateCategoryAsync(SaveGalleryCategoryRequest request, CancellationToken cancellationToken);
    Task<AdminGalleryCategoryResponse> UpdateCategoryAsync(Guid id, SaveGalleryCategoryRequest request, CancellationToken cancellationToken);
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<AdminGalleryItemResponse>> ListItemsAsync(Guid? categoryId, PageQuery query, CancellationToken cancellationToken);
    Task<AdminGalleryItemResponse> GetItemAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminGalleryItemResponse> CreateItemAsync(SaveGalleryItemRequest request, CancellationToken cancellationToken);
    Task<AdminGalleryItemResponse> UpdateItemAsync(Guid id, SaveGalleryItemRequest request, CancellationToken cancellationToken);
    Task DeleteItemAsync(Guid id, CancellationToken cancellationToken);
}
