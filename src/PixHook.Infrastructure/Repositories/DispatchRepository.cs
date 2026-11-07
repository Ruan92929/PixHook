using Microsoft.EntityFrameworkCore;
using PixHook.Domain.Entities;
using PixHook.Domain.Repositories;
using PixHook.Infrastructure.Persistence;

namespace PixHook.Infrastructure.Repositories;

internal sealed class DispatchRepository : IDispatchRepository
{
    private readonly PixHookDbContext _db;

    public DispatchRepository(PixHookDbContext db) => _db = db;

    public async Task AddAsync(WebhookDispatch dispatch, CancellationToken ct = default) =>
        await _db.Dispatches.AddAsync(dispatch, ct);

    public Task<WebhookDispatch?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Dispatches.AsNoTracking()
            .Include(d => d.Template)!.ThenInclude(t => t!.Provider)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<(IReadOnlyList<WebhookDispatch> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, Guid? providerId, CancellationToken ct = default)
    {
        var query = _db.Dispatches.AsNoTracking();
        if (providerId.HasValue)
            query = query.Where(d => d.Template!.ProviderId == providerId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .Include(d => d.Template)!.ThenInclude(t => t!.Provider)
            .OrderByDescending(d => d.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
