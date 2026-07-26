using System.Text.Json;
using System.Text.Json.Serialization;
using HBP.Api.Infrastructure;
using HBP.Application;
using HBP.Application.Abstractions;
using HBP.Infrastructure;
using HBP.Infrastructure.Persistence;
using HBP.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using HBP.Api.HostedServices;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((_, configuration) => configuration.WriteTo.Console(new RenderedCompactJsonFormatter()));

var connectionString = builder.Configuration.GetConnectionString("HbpDatabase")
    ?? throw new InvalidOperationException("Connection string 'HbpDatabase' was not found.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString, builder.Configuration);
builder.Services.AddScoped<IRequestLanguageAccessor, RequestLanguageAccessor>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<ValidationActionFilter>();
builder.Services.AddControllers(options => options.Filters.Add<ValidationActionFilter>()).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "hbp.admin";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Configuration.GetValue("Auth:CookieSecure", true)
        ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = false;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (origins.Length > 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));
builder.Services.AddHealthChecks().AddNpgSql(connectionString, name: "postgres");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        return ValueTask.CompletedTask;
    };
    options.AddPolicy("public-submit", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));
    options.AddPolicy("admin-sensitive", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));
});
builder.Services.Configure<EmailDispatchOptions>(builder.Configuration.GetSection("EmailDispatch"));
builder.Services.AddHostedService<EmailDispatchBackgroundService>();

var app = builder.Build();
// Production applies migrations as a separate pre-deploy step (`dotnet ef migrations bundle`);
// this switch exists for dev/staging containers that have no pre-deploy hook.
if (app.Configuration.GetValue("RUN_MIGRATIONS_ON_STARTUP", false))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<HbpDbContext>().Database.MigrateAsync();
}
if (app.Configuration.GetValue("Database:SeedOnStartup", app.Environment.IsDevelopment()))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<HbpDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var imageProcessor = scope.ServiceProvider.GetRequiredService<IImageProcessor>();
    var mediaStorage = scope.ServiceProvider.GetRequiredService<IMediaStorage>();
    await SeedData.InitializeAsync(db, hasher, imageProcessor, mediaStorage);
}
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors("Frontend");
var mediaRoot = Path.GetFullPath(builder.Configuration["Media:StorageRoot"] ?? "data/media");
Directory.CreateDirectory(mediaRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(mediaRoot),
    RequestPath = "/media",
    OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable"
});
app.UseMiddleware<LanguageResolutionMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<AdminCsrfMiddleware>();
app.UseAuthorization();
app.UseMiddleware<PublicCacheMiddleware>();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapHealthChecks("/health/ready");
app.Run();

public partial class Program;
