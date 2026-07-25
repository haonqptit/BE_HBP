using FluentValidation;
using HBP.Application.Common;

namespace HBP.Application.Admin;

public sealed record AdminAmenityResponse(Guid Id, string NameVi, string? NameJa, string? Icon,
    int DisplayOrder, bool IsVisible, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record SaveAmenityRequest(string NameVi, string? NameJa, string? Icon, int DisplayOrder, bool IsVisible);

public sealed class SaveAmenityRequestValidator : AbstractValidator<SaveAmenityRequest>
{
    public SaveAmenityRequestValidator()
    {
        RuleFor(x => x.NameVi).NotEmpty().MaximumLength(150);
        RuleFor(x => x.NameJa).MaximumLength(150);
        RuleFor(x => x.Icon).MaximumLength(100);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public interface IAdminAmenityService
{
    Task<PagedResult<AdminAmenityResponse>> ListAsync(PageQuery query, CancellationToken cancellationToken);
    Task<AdminAmenityResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminAmenityResponse> CreateAsync(SaveAmenityRequest request, CancellationToken cancellationToken);
    Task<AdminAmenityResponse> UpdateAsync(Guid id, SaveAmenityRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
