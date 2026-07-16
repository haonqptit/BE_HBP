using HBP.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace HBP.Infrastructure.Media;

public sealed class LocalFileMediaStorage(IOptions<MediaOptions> options, IClock clock) : IMediaStorage
{
    public async Task<StoredMediaPaths> SaveAsync(Guid mediaId, ProcessedImage image, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var relative = Path.Combine(now.Year.ToString("0000"), now.Month.ToString("00"), mediaId.ToString("N"));
        var directory = Path.GetFullPath(Path.Combine(options.Value.StorageRoot, relative));
        Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(Path.Combine(directory, "original.webp"), image.Original, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(directory, "medium.webp"), image.Medium, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(directory, "thumbnail.webp"), image.Thumbnail, cancellationToken);
        var url = $"{options.Value.BaseUrl.TrimEnd('/')}/{relative.Replace('\\', '/')}/original.webp";
        return new StoredMediaPaths(Path.Combine(directory, "original.webp"), Path.Combine(directory, "medium.webp"),
            Path.Combine(directory, "thumbnail.webp"), url);
    }

    public Task DeleteAsync(StoredMediaPaths paths, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(paths.Original);
        if (directory is not null && Directory.Exists(directory)) Directory.Delete(directory, true);
        return Task.CompletedTask;
    }
}
