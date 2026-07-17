using HBP.Application.Common;

namespace HBP.Application.Media;

public sealed record MediaResponse(Guid Id, string OriginalFileName, string PublicUrl, string MediumUrl,
    string ThumbnailUrl, string MimeType, long SizeBytes, int? Width, int? Height,
    string? AltTextVi, string? AltTextJa, DateTime CreatedAt);

public interface IMediaService
{
    Task<MediaResponse> UploadAsync(Stream stream, string fileName, string contentType, long length,
        string? altTextVi, string? altTextJa, CancellationToken cancellationToken);
    Task<PagedResult<MediaResponse>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<MediaResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
