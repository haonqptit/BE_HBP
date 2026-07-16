using HBP.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace HBP.Infrastructure.Persistence;

public sealed class HbpDbContextFactory : IDesignTimeDbContextFactory<HbpDbContext>
{
    public HbpDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("HBP_DESIGN_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=hbp;Username=hbp;Password=hbp_dev_password";

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<PriceDisplayMode>("price_display_mode", HbpDbContext.IdentityTranslator);
        dataSourceBuilder.MapEnum<BookingRequestStatus>("booking_request_status", HbpDbContext.IdentityTranslator);
        dataSourceBuilder.MapEnum<EmailStatus>("email_status", HbpDbContext.IdentityTranslator);
        dataSourceBuilder.MapEnum<LanguageCode>("language_code_enum", HbpDbContext.SnakeCaseTranslator);

        var options = new DbContextOptionsBuilder<HbpDbContext>()
            .UseNpgsql(dataSourceBuilder.Build())
            .Options;

        return new HbpDbContext(options);
    }
}
