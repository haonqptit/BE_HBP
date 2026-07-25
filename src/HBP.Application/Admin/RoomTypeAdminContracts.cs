using HBP.Application.Common;
using HBP.Domain.Enums;

namespace HBP.Application.Admin;

public sealed record AdminRoomTypeListItem(Guid Id, string Code, string Slug, string NameVi, string? NameJa,
    PriceDisplayMode PriceDisplayMode, decimal? PriceVnd, decimal? PriceUsd, int Capacity,
    int DisplayOrder, bool IsVisible, AdminMediaSummary? FeaturedMedia, DateTime UpdatedAt);

public sealed record AdminRoomTypeAmenityLink(Guid AmenityId, string NameVi, string? NameJa, int? DisplayOrder);

public sealed record AdminRoomTypeMediaLink(Guid MediaFileId, int DisplayOrder, AdminMediaSummary Media);

public sealed record AdminRoomTypeResponse(Guid Id, string Code, string Slug, string NameVi, string? NameJa,
    string? ShortDescriptionVi, string? ShortDescriptionJa, string? DescriptionVi, string? DescriptionJa,
    decimal? PriceVnd, decimal? PriceUsd, PriceDisplayMode PriceDisplayMode, int Capacity,
    decimal? AreaSquareMeters, string? BedDescriptionVi, string? BedDescriptionJa,
    Guid? FeaturedMediaId, AdminMediaSummary? FeaturedMedia, int DisplayOrder, bool IsVisible,
    string? SeoTitleVi, string? SeoTitleJa, string? SeoDescriptionVi, string? SeoDescriptionJa,
    IReadOnlyList<AdminRoomTypeAmenityLink> Amenities, IReadOnlyList<AdminRoomTypeMediaLink> Media,
    DateTime CreatedAt, DateTime UpdatedAt);

public sealed record SaveRoomTypeRequest(string Code, string Slug, string NameVi, string? NameJa,
    string? ShortDescriptionVi, string? ShortDescriptionJa, string? DescriptionVi, string? DescriptionJa,
    decimal? PriceVnd, decimal? PriceUsd, PriceDisplayMode PriceDisplayMode, int Capacity,
    decimal? AreaSquareMeters, string? BedDescriptionVi, string? BedDescriptionJa,
    Guid? FeaturedMediaId, int DisplayOrder, bool IsVisible,
    string? SeoTitleVi, string? SeoTitleJa, string? SeoDescriptionVi, string? SeoDescriptionJa);

public interface IAdminRoomTypeService
{
    Task<PagedResult<AdminRoomTypeListItem>> ListAsync(PageQuery query, CancellationToken cancellationToken);
    Task<AdminRoomTypeResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminRoomTypeResponse> CreateAsync(SaveRoomTypeRequest request, CancellationToken cancellationToken);
    Task<AdminRoomTypeResponse> UpdateAsync(Guid id, SaveRoomTypeRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<AdminRoomTypeResponse> ReplaceAmenitiesAsync(Guid id, ReplaceLinksRequest request, CancellationToken cancellationToken);
    Task<AdminRoomTypeResponse> ReplaceMediaAsync(Guid id, ReplaceLinksRequest request, CancellationToken cancellationToken);
}
