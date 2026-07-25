using System.Text.Json.Serialization;
using HBP.Domain.Enums;

namespace HBP.Application.Public;

public sealed record ImageResponse(string Original, string Medium, string Thumbnail, string? Alt);
public sealed record SeoResponse(string? Title, string? Description);
public sealed record AmenityResponse(Guid Id, string Name, string? Icon);
public sealed record RoomTypeListItemResponse(Guid Id, string Slug, string Name, string? ShortDescription,
    int Capacity, decimal? AreaSquareMeters, string? BedDescription, PriceDisplayMode PriceDisplayMode,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? PriceVnd,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? PriceUsd,
    ImageResponse? FeaturedImage, int DisplayOrder);
public sealed record RoomTypeDetailResponse(Guid Id, string Code, string Slug, string Name,
    string? ShortDescription, string? Description, int Capacity, decimal? AreaSquareMeters,
    string? BedDescription, PriceDisplayMode PriceDisplayMode,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? PriceVnd,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? PriceUsd,
    ImageResponse? FeaturedImage, IReadOnlyList<AmenityResponse> Amenities,
    IReadOnlyList<ImageResponse> Media, SeoResponse Seo);
public sealed record ServiceListItemResponse(Guid Id, string Slug, string Name,
    string? ShortDescription, string? PriceNote, ImageResponse? FeaturedImage, int DisplayOrder);
public sealed record ServiceDetailResponse(Guid Id, string Slug, string Name,
    string? ShortDescription, string? Description, string? PriceNote, ImageResponse? FeaturedImage);
public sealed record GalleryItemResponse(Guid Id, ImageResponse Image, string? Caption, int DisplayOrder);
public sealed record GalleryCategoryResponse(Guid Id, string Slug, string Name, int DisplayOrder,
    IReadOnlyList<GalleryItemResponse> Items);
public sealed record SiteMetadataResponse(string Name, string Address, string Phone, string Email,
    string CheckIn, string CheckOut, string Reception);

public interface IPublicRoomTypeQueryService
{
    Task<IReadOnlyList<RoomTypeListItemResponse>> ListAsync(CancellationToken cancellationToken);
    Task<RoomTypeDetailResponse> GetAsync(string slug, CancellationToken cancellationToken);
}
public interface IPublicServiceQueryService
{
    Task<IReadOnlyList<ServiceListItemResponse>> ListAsync(CancellationToken cancellationToken);
    Task<ServiceDetailResponse> GetAsync(string slug, CancellationToken cancellationToken);
}
public interface IPublicGalleryQueryService
{
    Task<IReadOnlyList<GalleryCategoryResponse>> ListAsync(string? category, CancellationToken cancellationToken);
}
public interface IPublicAmenityQueryService
{
    Task<IReadOnlyList<AmenityResponse>> ListAsync(CancellationToken cancellationToken);
}
public interface IPublicSiteMetadataQueryService
{
    Task<SiteMetadataResponse> GetAsync(CancellationToken cancellationToken);
}
