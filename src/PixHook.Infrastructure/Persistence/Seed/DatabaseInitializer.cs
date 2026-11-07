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
        var existingScenarios = await db.Scenarios.Select(s => s.Id).ToListAsync(ct);
        var existingScenarioSteps = await db.ScenarioSteps.Select(s => s.Id).ToListAsync(ct);

        var newProviders = SeedData.Providers().Where(p => !existingProviders.Contains(p.Id)).ToList();
        var newTemplates = SeedData.Templates().Where(t => !existingTemplates.Contains(t.Id)).ToList();
        var newScenarios = SeedData.Scenarios().Where(s => !existingScenarios.Contains(s.Id)).ToList();
        var newScenarioSteps = SeedData.ScenarioSteps().Where(s => !existingScenarioSteps.Contains(s.Id)).ToList();

        if (newProviders.Count == 0 && newTemplates.Count == 0 && newScenarios.Count == 0 && newScenarioSteps.Count == 0)
            return;

        db.Providers.AddRange(newProviders);
        db.EventTemplates.AddRange(newTemplates);
        // Templates precisam existir antes dos passos de cenário (FK ScenarioSteps.TemplateId).
        await db.SaveChangesAsync(ct);

        db.Scenarios.AddRange(newScenarios);
        await db.SaveChangesAsync(ct);

        db.ScenarioSteps.AddRange(newScenarioSteps);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seed aplicado: {Providers} provedor(es), {Templates} template(s), {Scenarios} cenário(s), {Steps} passo(s) de cenário.",
            newProviders.Count, newTemplates.Count, newScenarios.Count, newScenarioSteps.Count);
    }
}
