namespace PixHook.Application.Abstractions;

/// <summary>
/// Envia o webhook para a URL de destino. Hoje síncrono via HTTP; no futuro pode ser
/// trocado por fila/outbox sem alterar os handlers.
/// </summary>
public interface IWebhookDispatcher
{
    Task<DispatchResult> SendAsync(string url, string payload, IDictionary<string, string> headers, CancellationToken ct = default);
}

/// <summary>Resultado técnico do envio HTTP.</summary>
public sealed record DispatchResult(
    bool Success,
    int? StatusCode,
    string? ResponseBody,
    long ResponseTimeMs,
    string? ErrorMessage)
{
    public static DispatchResult Ok(int statusCode, string? body, long elapsedMs) =>
        new(true, statusCode, body, elapsedMs, null);

    public static DispatchResult HttpError(int statusCode, string? body, long elapsedMs) =>
        new(false, statusCode, body, elapsedMs, $"Destino respondeu HTTP {statusCode}.");

    public static DispatchResult Failure(string error, long elapsedMs) =>
        new(false, null, null, elapsedMs, error);
}
