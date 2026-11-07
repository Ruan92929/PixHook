using PixHook.Domain.Entities;

namespace PixHook.Domain.Repositories;

public interface IScenarioRepository
{
    Task<IReadOnlyList<DispatchScenario>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>Retorna o cenário com os passos (e o Template/Provider de cada passo) carregados.</summary>
    Task<DispatchScenario?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
