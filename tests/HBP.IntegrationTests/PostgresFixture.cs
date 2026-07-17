using HBP.Infrastructure;
using HBP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace HBP.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private string? _previousConnectionString;
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("hbp_tests")
        .WithUsername("hbp")
        .WithPassword("hbp_tests")
        .Build();

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(Container.GetConnectionString());
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<HbpDbContext>().Database.MigrateAsync();

        _previousConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__HbpDatabase");
        Environment.SetEnvironmentVariable("ConnectionStrings__HbpDatabase", Container.GetConnectionString());
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:HbpDatabase"] = Container.GetConnectionString(),
                    ["Database:SeedOnStartup"] = "false",
                    ["Auth:CookieSecure"] = "false",
                    ["Media:StorageRoot"] = Path.Combine(Path.GetTempPath(), "hbp-tests", Guid.NewGuid().ToString("N")),
                    ["Cors:AllowedOrigins:0"] = "http://localhost"
                }));
        });
        _ = Factory.Server;
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__HbpDatabase", _previousConnectionString);
        await Container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
