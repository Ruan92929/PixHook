namespace PixHook.Infrastructure.Dispatching;

public sealed class DispatcherOptions
{
    public const string SectionName = "Dispatcher";

    /// <summary>Timeout por tentativa, em segundos.</summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>Número de novas tentativas em falhas transitórias (5xx, 408, rede).</summary>
    public int RetryCount { get; set; } = 2;

    /// <summary>Tamanho máximo do corpo de resposta armazenado.</summary>
    public int MaxResponseBodyLength { get; set; } = 8_000;
}
