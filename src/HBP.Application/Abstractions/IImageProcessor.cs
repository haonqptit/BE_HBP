namespace HBP.Application.Abstractions;

public interface IImageProcessor
{
    Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken cancellationToken);
}

public sealed record ProcessedImage(
    int Width,
    int Height,
    byte[] Original,
    byte[] Medium,
    byte[] Thumbnail);
