using Microsoft.EntityFrameworkCore;
using PixHook.Domain.Entities;
using PixHook.Domain.Repositories;
using PixHook.Infrastructure.Persistence;

namespace PixHook.Infrastructure.Repositories;

internal sealed class ScenarioRepository : IScenarioRepository
{
    private readonly PixHookDbContext _db;

    public ScenarioRepository(PixHookDbContext db) => _db = db;

    public async Task<IReadOnlyList<DispatchScenario>> GetActiveAsync(CancellationToken ct = default) =>
        await _db.Scenarios.AsNoTracking()
            .Where(s => s.IsActive)
            .Include(s => s.Steps)!.ThenInclude(step => step.Template)!.ThenInclude(t => t!.Provider)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public Task<DispatchScenario?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Scenarios.AsNoTracking()
            .Include(s => s.Steps)!.ThenInclude(step => step.Template)!.ThenInclude(t => t!.Provider)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
}
