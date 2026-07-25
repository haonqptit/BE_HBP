using HBP.Domain.Enums;
using HBP.Infrastructure.Persistence;
using HBP.Application.Abstractions;
using HBP.Application.Auth;
using HBP.Infrastructure.Auth;
using HBP.Infrastructure.Common;
using HBP.Application.Admin;
using HBP.Infrastructure.Admin;
using HBP.Application.Media;
using HBP.Infrastructure.Media;
using Microsoft.Extensions.Configuration;
using HBP.Application.Public;
using HBP.Infrastructure.Public;
using HBP.Application.Requests;
using HBP.Infrastructure.Requests;
using HBP.Application.Email;
using HBP.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HBP.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers <see cref="HbpDbContext"/> and its PostgreSQL data source (with enum mappings)
    /// using the supplied connection string. Building the data source does not open a connection,
    /// so this is safe to call without a running database.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, IConfiguration? configuration = null)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

        dataSourceBuilder.MapEnum<PriceDisplayMode>("price_display_mode", HbpDbContext.IdentityTranslator);
        dataSourceBuilder.MapEnum<BookingRequestStatus>("booking_request_status", HbpDbContext.IdentityTranslator);
        dataSourceBuilder.MapEnum<EmailStatus>("email_status", HbpDbContext.IdentityTranslator);
        dataSourceBuilder.MapEnum<LanguageCode>("language_code_enum", HbpDbContext.SnakeCaseTranslator);

        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<HbpDbContext>(options => options.UseNpgsql(dataSource));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IImageProcessor, ImageSharpImageProcessor>();
        services.AddSingleton<IMediaStorage, LocalFileMediaStorage>();
        services.AddScoped<IMediaService, MediaService>();
        if (configuration is not null) services.Configure<MediaOptions>(configuration.GetSection("Media"));
        services.AddScoped<IPublicRoomTypeQueryService, PublicRoomTypeQueryService>();
        services.AddScoped<IPublicServiceQueryService, PublicServiceQueryService>();
        services.AddScoped<IPublicGalleryQueryService, PublicGalleryQueryService>();
        services.AddScoped<IPublicAmenityQueryService, PublicAmenityQueryService>();
        services.AddSingleton<IReferenceCodeGenerator, ReferenceCodeGenerator>();
        services.AddScoped<IBookingRequestService, BookingRequestService>();
        services.AddScoped<IContactRequestService, ContactRequestService>();
        services.AddScoped<IAdminRoomTypeService, AdminRoomTypeService>();
        services.AddScoped<IAdminAmenityService, AdminAmenityService>();
        services.AddScoped<IAdminServiceCatalogService, AdminServiceCatalogService>();
        services.AddScoped<IAdminGalleryService, AdminGalleryService>();
        services.AddScoped<IAdminBookingRequestService, AdminBookingRequestService>();
        services.AddScoped<IAdminContactRequestService, AdminContactRequestService>();
        services.AddScoped<IAdminSystemSettingService, AdminSystemSettingService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IEmailTemplateRenderer, ScribanEmailTemplateRenderer>();
        if (configuration is not null) services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));

        return services;
    }
}
