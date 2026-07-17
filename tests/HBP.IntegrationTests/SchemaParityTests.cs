using Npgsql;

namespace HBP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class SchemaParityTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Migration_CreatesRequiredSchemaObjects()
    {
        await using var connection = new NpgsqlConnection(fixture.Container.GetConnectionString());
        await connection.OpenAsync();

        Assert.Equal(2, await Scalar<long>(connection,
            "SELECT count(*) FROM pg_extension WHERE extname IN ('pgcrypto','pg_trgm')"));
        Assert.Equal(10, await Scalar<long>(connection,
            "SELECT count(DISTINCT trigger_name) FROM information_schema.triggers WHERE trigger_schema='public' AND trigger_name LIKE 'trg_%'"));
        Assert.Equal(2, await Scalar<long>(connection,
            "SELECT count(*) FROM pg_proc WHERE proname IN ('set_updated_at','normalize_email')"));
        Assert.Equal(3, await Scalar<long>(connection,
            "SELECT count(*) FROM information_schema.columns WHERE table_name='admin_users' AND column_name IN ('failed_count','first_failed_at','locked_until')"));
        Assert.Equal(5, await Scalar<long>(connection,
            "SELECT count(*) FROM pg_indexes WHERE schemaname='public' AND indexname LIKE '%_trgm'"));
    }

    private static async Task<T> Scalar<T>(NpgsqlConnection connection, string sql) =>
        (T)(await new NpgsqlCommand(sql, connection).ExecuteScalarAsync())!;
}
