using Microsoft.EntityFrameworkCore;
using Splitwise.Infrastructure.Persistence;

namespace Splitwise.Api.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Застосовує міграції EF Core під час старту (керується Database:MigrateOnStartup).
    /// Якщо міграцій у проєкті немає — створює схему через EnsureCreated (запасний варіант).
    /// </summary>
    public static void ApplyDatabaseMigrations(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Database:MigrateOnStartup", true))
            return;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SplitwiseDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

        if (db.Database.GetMigrations().Any())
        {
            logger.LogInformation("Застосування міграцій бази даних...");
            db.Database.Migrate();
        }
        else
        {
            logger.LogWarning(
                "У проєкті немає міграцій EF Core — схему створено через EnsureCreated. " +
                "Створіть міграцію: dotnet ef migrations add InitialPostgres.");
            db.Database.EnsureCreated();
        }
    }
}
