using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace HBP.Api.Infrastructure;

public sealed class AdminCsrfMiddleware(RequestDelegate next)
{
    public const string CookieName = "hbp.csrf";
    public const string HeaderName = "X-HBP-CSRF";

    public async Task InvokeAsync(HttpContext context)
    {
        var unsafeMethod = !HttpMethods.IsGet(context.Request.Method)
            && !HttpMethods.IsHead(context.Request.Method)
            && !HttpMethods.IsOptions(context.Request.Method);
        var adminPath = context.Request.Path.StartsWithSegments("/api/admin");
        var loginPath = context.Request.Path.Equals("/api/admin/auth/login");

        if (unsafeMethod && adminPath && !loginPath && context.User.Identity?.IsAuthenticated == true)
        {
            var cookie = context.Request.Cookies[CookieName];
            var header = context.Request.Headers[HeaderName].FirstOrDefault();
            if (string.IsNullOrEmpty(cookie) || string.IsNullOrEmpty(header)
                || !CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(cookie), System.Text.Encoding.UTF8.GetBytes(header)))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { title = "Invalid CSRF token", status = 400 });
                return;
            }
        }
        await next(context);
    }
}
