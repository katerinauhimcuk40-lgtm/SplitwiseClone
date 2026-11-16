using Microsoft.Extensions.Configuration;
using Npgsql;
using Splitwise.Api.Configuration;

namespace Splitwise.Tests.Api;

public class ConnectionStringResolverTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void Normalize_NeonUri_ConvertsToNpgsqlFormat()
    {
        var uri = "postgresql://neon_user:p%40ss@ep-cool-123.eu-central-1.aws.neon.tech/neondb?sslmode=require&channel_binding=require";

        var result = new NpgsqlConnectionStringBuilder(ConnectionStringResolver.Normalize(uri));

        Assert.Equal("ep-cool-123.eu-central-1.aws.neon.tech", result.Host);
        Assert.Equal(5432, result.Port);
        Assert.Equal("neondb", result.Database);
        Assert.Equal("neon_user", result.Username);
        Assert.Equal("p@ss", result.Password);
        Assert.Equal(SslMode.Require, result.SslMode);
    }

    [Fact]
    public void Normalize_UriWithPortAndPostgresScheme_IsSupported()
    {
        var result = new NpgsqlConnectionStringBuilder(
            ConnectionStringResolver.Normalize("postgres://u:p@db.example.com:6543/app"));

        Assert.Equal("db.example.com", result.Host);
        Assert.Equal(6543, result.Port);
        Assert.Equal("app", result.Database);
    }

    [Fact]
    public void Normalize_UriWithoutSslMode_DefaultsToRequire()
    {
        var result = new NpgsqlConnectionStringBuilder(
            ConnectionStringResolver.Normalize("postgresql://u:p@host/db"));

        Assert.Equal(SslMode.Require, result.SslMode);
    }

    [Fact]
    public void Normalize_KeyValueString_IsReturnedUnchanged()
    {
        const string conn = "Host=localhost;Database=splitwise;Username=postgres;Password=postgres";

        Assert.Equal(conn, ConnectionStringResolver.Normalize(conn));
    }

    [Fact]
    public void Resolve_PrefersDatabaseUrlOverDefaultConnection()
    {
        var config = Config(
            ("DATABASE_URL", "postgresql://u:p@neon-host/neondb"),
            ("ConnectionStrings:DefaultConnection", "Host=localhost;Database=local"));

        var result = new NpgsqlConnectionStringBuilder(ConnectionStringResolver.Resolve(config));

        Assert.Equal("neon-host", result.Host);
    }

    [Fact]
    public void Resolve_FallsBackToDefaultConnection()
    {
        var config = Config(("ConnectionStrings:DefaultConnection", "Host=localhost;Database=local"));

        var result = new NpgsqlConnectionStringBuilder(ConnectionStringResolver.Resolve(config));

        Assert.Equal("localhost", result.Host);
        Assert.Equal("local", result.Database);
    }

    [Fact]
    public void Resolve_NothingConfigured_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ConnectionStringResolver.Resolve(Config()));
    }
}
