using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PixHook.Infrastructure.Persistence.Seed;

public static class DatabaseInitializer
{
    /// <summary>Aplica migrations pendentes e executa o seed idempotente.</summary>
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PixHookDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        await db.Database.MigrateAsync(ct);
        await SeedAsync(db, logger, ct);
    }

    internal static async Task SeedAsync(PixHookDbContext db, ILogger logger, CancellationToken ct)
    {
        var existingProviders = await db.Providers.Select(p => p.Id).ToListAsync(ct);
        var existingTemplates = await db.EventTemplates.Select(t => t.Id).ToListAsync(ct);

        var newProviders = SeedData.Providers().Where(p => !existingProviders.Contains(p.Id)).ToList();
        var newTemplates = SeedData.Templates().Where(t => !existingTemplates.Contains(t.Id)).ToList();

        if (newProviders.Count == 0 && newTemplates.Count == 0) return;

        db.Providers.AddRange(newProviders);
        db.EventTemplates.AddRange(newTemplates);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seed aplicado: {Providers} provedor(es), {Templates} template(s).",
            newProviders.Count, newTemplates.Count);
    }
}
