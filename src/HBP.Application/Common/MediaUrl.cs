namespace HBP.Application.Common;

public static class MediaUrl
{
    public static string Variant(string originalUrl, string variant)
    {
        var separator = originalUrl.LastIndexOf('/');
        return separator < 0 ? $"{variant}.webp" : originalUrl[..(separator + 1)] + variant + ".webp";
    }
}
