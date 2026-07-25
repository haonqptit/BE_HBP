using FluentValidation;
using HBP.Application.Common;

namespace HBP.Application.Admin;

public sealed record AdminServiceResponse(Guid Id, string Slug, string NameVi, string? NameJa,
    string? ShortDescriptionVi, string? ShortDescriptionJa, string? DescriptionVi, string? DescriptionJa,
    string? PriceNoteVi, string? PriceNoteJa, Guid? FeaturedMediaId, AdminMediaSummary? FeaturedMedia,
    int DisplayOrder, bool IsVisible, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record SaveServiceRequest(string Slug, string NameVi, string? NameJa,
    string? ShortDescriptionVi, string? ShortDescriptionJa, string? DescriptionVi, string? DescriptionJa,
    string? PriceNoteVi, string? PriceNoteJa, Guid? FeaturedMediaId, int DisplayOrder, bool IsVisible);

public sealed class SaveServiceRequestValidator : AbstractValidator<SaveServiceRequest>
{
    public SaveServiceRequestValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(150).Matches(AdminSlug.Pattern).WithMessage(AdminSlug.Message);
        RuleFor(x => x.NameVi).NotEmpty().MaximumLength(255);
        RuleFor(x => x.NameJa).MaximumLength(255);
        RuleFor(x => x.ShortDescriptionVi).MaximumLength(500);
        RuleFor(x => x.ShortDescriptionJa).MaximumLength(500);
        RuleFor(x => x.PriceNoteVi).MaximumLength(255);
        RuleFor(x => x.PriceNoteJa).MaximumLength(255);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public interface IAdminServiceCatalogService
{
    Task<PagedResult<AdminServiceResponse>> ListAsync(PageQuery query, CancellationToken cancellationToken);
    Task<AdminServiceResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminServiceResponse> CreateAsync(SaveServiceRequest request, CancellationToken cancellationToken);
    Task<AdminServiceResponse> UpdateAsync(Guid id, SaveServiceRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
