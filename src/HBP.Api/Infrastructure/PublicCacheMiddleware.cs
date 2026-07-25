using System.Security.Cryptography;

namespace HBP.Api.Infrastructure;

public sealed class PublicCacheMiddleware(RequestDelegate next)
{
    private static readonly PathString[] Paths =
        ["/api/rooms", "/api/services", "/api/gallery", "/api/amenities", "/api/site-metadata"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) || !Paths.Any(x => context.Request.Path.StartsWithSegments(x)))
        { await next(context); return; }

        var original = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await next(context);
            if (context.Response.StatusCode != StatusCodes.Status200OK) { buffer.Position = 0; await buffer.CopyToAsync(original); return; }
            var body = buffer.ToArray();
            var etag = $"W/\"{Convert.ToBase64String(SHA256.HashData(body))}\"";
            context.Response.Headers.ETag = etag;
            context.Response.Headers.CacheControl = "public,max-age=60";
            context.Response.Headers.Vary = "Accept-Language";
            if (context.Request.Headers.IfNoneMatch.Any(x => x == etag))
            { context.Response.StatusCode = StatusCodes.Status304NotModified; context.Response.ContentLength = 0; return; }
            await original.WriteAsync(body);
        }
        finally { context.Response.Body = original; }
    }
}
