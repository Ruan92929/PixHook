using PixHook.Domain.Entities;

namespace PixHook.Domain.Repositories;

public interface IProviderRepository
{
    Task<IReadOnlyList<WebhookProvider>> GetActiveAsync(CancellationToken ct = default);
    Task<WebhookProvider?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<WebhookEventTemplate>> GetTemplatesByProviderAsync(Guid providerId, CancellationToken ct = default);

    /// <summary>Retorna o template com o <see cref="WebhookEventTemplate.Provider"/> carregado.</summary>
    Task<WebhookEventTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken ct = default);
}
