using Microsoft.EntityFrameworkCore;
using PixHook.Domain.Entities;

namespace PixHook.Infrastructure.Persistence;

public class PixHookDbContext : DbContext
{
    public PixHookDbContext(DbContextOptions<PixHookDbContext> options) : base(options) { }

    public DbSet<WebhookProvider> Providers => Set<WebhookProvider>();
    public DbSet<WebhookEventTemplate> EventTemplates => Set<WebhookEventTemplate>();
    public DbSet<WebhookDispatch> Dispatches => Set<WebhookDispatch>();
    public DbSet<DispatchScenario> Scenarios => Set<DispatchScenario>();
    public DbSet<DispatchScenarioStep> ScenarioSteps => Set<DispatchScenarioStep>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PixHookDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
