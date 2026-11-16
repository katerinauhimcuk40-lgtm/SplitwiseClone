using System.Web;
using Npgsql;

namespace Splitwise.Api.Configuration;

/// <summary>
/// Визначає рядок підключення до PostgreSQL.
/// Пріоритет: змінна середовища DATABASE_URL (Render/Neon) -> ConnectionStrings:DefaultConnection.
/// Підтримує як формат Npgsql ("Host=...;Database=..."), так і URI з Neon/Heroku
/// ("postgresql://user:password@host/db?sslmode=require").
/// </summary>
public static class ConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var raw = configuration["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(raw))
            raw = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException(
                "Не задано рядок підключення до БД. Встановіть змінну середовища DATABASE_URL " +
                "або ConnectionStrings:DefaultConnection.");

        return Normalize(raw);
    }

    public static string Normalize(string value)
    {
        var isUri = value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
                    || value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
        if (!isUri)
            return value;

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);
        var query = HttpUtility.ParseQueryString(uri.Query);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            // Neon вимагає TLS; sslmode з URI враховується, якщо вказаний.
            SslMode = ParseSslMode(query["sslmode"])
        };

        return builder.ConnectionString;
    }

    private static SslMode ParseSslMode(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && Enum.TryParse<SslMode>(value.Replace("-", string.Empty), ignoreCase: true, out var mode)
            ? mode
            : SslMode.Require;
}
