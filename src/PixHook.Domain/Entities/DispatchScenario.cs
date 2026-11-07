namespace PixHook.Domain.Entities;

/// <summary>
/// Agregado raiz: uma sequência ordenada de eventos a disparar, cada um com um atraso em relação
/// ao passo anterior. Serve para simular situações que um único disparo não cobre — por exemplo,
/// o mesmo webhook chegando duplicado, ou um evento de expiração chegando atrasado, depois da
/// confirmação. O ponto é testar como o seu sistema reage à sequência, não só a um evento isolado.
/// </summary>
public class DispatchScenario
{
    private readonly List<DispatchScenarioStep> _steps = new();

    private DispatchScenario() { } // EF Core

    public DispatchScenario(Guid id, string name, string slug, string description, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nome é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug é obrigatório.", nameof(slug));

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description ?? string.Empty;
        IsActive = isActive;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<DispatchScenarioStep> Steps => _steps.AsReadOnly();

    public DispatchScenarioStep AddStep(DispatchScenarioStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (step.ScenarioId != Id)
            throw new InvalidOperationException("Passo pertence a outro cenário.");
        _steps.Add(step);
        return step;
    }
}
