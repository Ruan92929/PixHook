using PixHook.Domain.Entities;
using PixHook.Domain.Enums;

namespace PixHook.Application.Features.Scenarios;

/// <summary>Um passo de um cenário: evento, atraso em relação ao passo anterior e o que ele simula.</summary>
public sealed record ScenarioStepDto(
    Guid Id,
    int Order,
    Guid TemplateId,
    string? TemplateName,
    EventType? EventType,
    string? ProviderName,
    int DelayBeforeMs,
    string Note)
{
    public static ScenarioStepDto From(DispatchScenarioStep s) => new(
        s.Id, s.Order, s.TemplateId, s.Template?.Name, s.Template?.EventType, s.Template?.Provider?.Name,
        s.DelayBeforeMs, s.Note);
}

/// <summary>Um cenário: sequência ordenada de eventos para testar como o seu sistema reage a ela.</summary>
public sealed record ScenarioDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    bool IsActive,
    IReadOnlyList<ScenarioStepDto> Steps)
{
    public static ScenarioDto From(DispatchScenario s) => new(
        s.Id, s.Name, s.Slug, s.Description, s.IsActive,
        s.Steps.OrderBy(step => step.Order).Select(ScenarioStepDto.From).ToList());
}
