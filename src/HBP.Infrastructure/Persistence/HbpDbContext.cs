using HBP.Domain.Entities;
using HBP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.NameTranslation;

namespace HBP.Infrastructure.Persistence;

public class HbpDbContext : DbContext
{
    // Name translator instances reused for both model (HasPostgresEnum) and runtime (MapEnum) mapping.
    internal static readonly INpgsqlNameTranslator IdentityTranslator = new IdentityNameTranslator();
    internal static readonly INpgsqlNameTranslator SnakeCaseTranslator = new NpgsqlSnakeCaseNameTranslator();

    public HbpDbContext(DbContextOptions<HbpDbContext> options) : base(options)
    {
    }

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<RoomTypeAmenity> RoomTypeAmenities => Set<RoomTypeAmenity>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<RoomTypeMedia> RoomTypeMedia => Set<RoomTypeMedia>();
    public DbSet<GalleryCategory> GalleryCategories => Set<GalleryCategory>();
    public DbSet<GalleryItem> GalleryItems => Set<GalleryItem>();
    public DbSet<BookingRequest> BookingRequests => Set<BookingRequest>();
    public DbSet<ContactRequest> ContactRequests => Set<ContactRequest>();
    public DbSet<EmailDelivery> EmailDeliveries => Set<EmailDelivery>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PostgreSQL enum types. Uppercase-label enums use verbatim (identity) translation;
        // language_code_enum uses snake-case so Vi -> vi, Ja -> ja.
        modelBuilder.HasPostgresEnum<PriceDisplayMode>("public", "price_display_mode", IdentityTranslator);
        modelBuilder.HasPostgresEnum<BookingRequestStatus>("public", "booking_request_status", IdentityTranslator);
        modelBuilder.HasPostgresEnum<EmailStatus>("public", "email_status", IdentityTranslator);
        modelBuilder.HasPostgresEnum<LanguageCode>("public", "language_code_enum", SnakeCaseTranslator);

        // Required PostgreSQL extensions (see schema.sql).
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HbpDbContext).Assembly);
    }
}
