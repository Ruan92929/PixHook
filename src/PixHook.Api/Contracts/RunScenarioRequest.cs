namespace PixHook.Api.Contracts;

/// <summary>Corpo para executar um cenário (sequência de eventos).</summary>
public sealed class RunScenarioRequest
{
    /// <summary>URL que receberá todos os webhooks do cenário.</summary>
    public string DestinationUrl { get; init; } = string.Empty;

    /// <summary>Headers HTTP adicionais, aplicados em todos os passos do cenário.</summary>
    public Dictionary<string, string>? CustomHeaders { get; init; }
}
