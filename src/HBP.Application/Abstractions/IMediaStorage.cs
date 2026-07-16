namespace HBP.Application.Abstractions;

public interface IMediaStorage
{
    Task<StoredMediaPaths> SaveAsync(Guid mediaId, ProcessedImage image, CancellationToken cancellationToken);
    Task DeleteAsync(StoredMediaPaths paths, CancellationToken cancellationToken);
}

public sealed record StoredMediaPaths(string Original, string Medium, string Thumbnail, string PublicUrl);
