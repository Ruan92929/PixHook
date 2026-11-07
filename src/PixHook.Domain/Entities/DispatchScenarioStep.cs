namespace PixHook.Domain.Entities;

/// <summary>
/// Um passo de um <see cref="DispatchScenario"/>: qual evento disparar e quanto tempo esperar
/// depois do passo anterior antes de disparar este (0 no primeiro passo).
/// </summary>
public class DispatchScenarioStep
{
    private DispatchScenarioStep() { } // EF Core

    public DispatchScenarioStep(Guid id, Guid scenarioId, int order, Guid templateId, int delayBeforeMs, string note)
    {
        if (order < 0) throw new ArgumentOutOfRangeException(nameof(order), "Ordem não pode ser negativa.");
        if (delayBeforeMs < 0) throw new ArgumentOutOfRangeException(nameof(delayBeforeMs), "Atraso não pode ser negativo.");

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        ScenarioId = scenarioId;
        Order = order;
        TemplateId = templateId;
        DelayBeforeMs = delayBeforeMs;
        Note = note ?? string.Empty;
    }

    public Guid Id { get; private set; }
    public Guid ScenarioId { get; private set; }
    public DispatchScenario? Scenario { get; private set; }
    public int Order { get; private set; }
    public Guid TemplateId { get; private set; }
    public WebhookEventTemplate? Template { get; private set; }

    /// <summary>Quanto esperar, em milissegundos, depois do passo anterior antes de disparar este.</summary>
    public int DelayBeforeMs { get; private set; }

    /// <summary>O que esse passo simula (ex.: "reenvio duplicado do mesmo evento").</summary>
    public string Note { get; private set; } = string.Empty;
}
