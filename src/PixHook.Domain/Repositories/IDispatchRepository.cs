using PixHook.Domain.Entities;

namespace PixHook.Domain.Repositories;

public interface IDispatchRepository
{
    Task AddAsync(WebhookDispatch dispatch, CancellationToken ct = default);

    /// <summary>Retorna o disparo com Template e Provider carregados.</summary>
    Task<WebhookDispatch?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<(IReadOnlyList<WebhookDispatch> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, Guid? providerId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
