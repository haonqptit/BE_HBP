namespace HBP.Infrastructure.Media;

public sealed class MediaOptions
{
    public string StorageRoot { get; set; } = "data/media";
    public string BaseUrl { get; set; } = "/media";
}
