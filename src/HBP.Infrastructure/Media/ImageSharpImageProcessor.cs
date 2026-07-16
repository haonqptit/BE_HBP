using HBP.Application.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace HBP.Infrastructure.Media;

public sealed class ImageSharpImageProcessor : IImageProcessor
{
    public async Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken cancellationToken)
    {
        using var image = await Image.LoadAsync(input, cancellationToken);
        var width = image.Width;
        var height = image.Height;
        return new ProcessedImage(width, height,
            await EncodeAsync(image, null, cancellationToken),
            await EncodeAsync(image, 800, cancellationToken),
            await EncodeAsync(image, 400, cancellationToken));
    }

    private static async Task<byte[]> EncodeAsync(Image source, int? maxWidth, CancellationToken cancellationToken)
    {
        using var clone = source.Clone(x =>
        {
            if (maxWidth is not null && source.Width > maxWidth)
                x.Resize(new ResizeOptions { Size = new Size(maxWidth.Value, 0), Mode = ResizeMode.Max });
        });
        await using var output = new MemoryStream();
        await clone.SaveAsync(output, new WebpEncoder { Quality = 82 }, cancellationToken);
        return output.ToArray();
    }
}
