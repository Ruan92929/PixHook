using Microsoft.EntityFrameworkCore;
using PixHook.Domain.Entities;
using PixHook.Domain.Repositories;
using PixHook.Infrastructure.Persistence;

namespace PixHook.Infrastructure.Repositories;

internal sealed class ProviderRepository : IProviderRepository
{
    private readonly PixHookDbContext _db;

    public ProviderRepository(PixHookDbContext db) => _db = db;

    public async Task<IReadOnlyList<WebhookProvider>> GetActiveAsync(CancellationToken ct = default) =>
        await _db.Providers.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync(ct);

    public Task<WebhookProvider?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<WebhookEventTemplate>> GetTemplatesByProviderAsync(Guid providerId, CancellationToken ct = default) =>
        await _db.EventTemplates.AsNoTracking()
            .Where(t => t.ProviderId == providerId)
            .OrderBy(t => t.EventType)
            .ToListAsync(ct);

    public Task<WebhookEventTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken ct = default) =>
        _db.EventTemplates.AsNoTracking().Include(t => t.Provider).FirstOrDefaultAsync(t => t.Id == templateId, ct);
}
