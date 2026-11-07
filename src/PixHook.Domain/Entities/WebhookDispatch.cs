namespace PixHook.Domain.Entities;

/// <summary>Registro (histórico) de cada disparo de webhook.</summary>
public class WebhookDispatch
{
    private WebhookDispatch() { } // EF Core

    private WebhookDispatch(
        Guid templateId,
        string destinationUrl,
        string renderedPayload,
        string renderedHeaders,
        DateTimeOffset requestedAt,
        Guid? replayOfDispatchId)
    {
        Id = Guid.NewGuid();
        TemplateId = templateId;
        DestinationUrl = destinationUrl;
        RenderedPayload = renderedPayload;
        RenderedHeaders = renderedHeaders;
        RequestedAt = requestedAt;
        ReplayOfDispatchId = replayOfDispatchId;
    }

    public Guid Id { get; private set; }
    public Guid TemplateId { get; private set; }
    public WebhookEventTemplate? Template { get; private set; }
    public string DestinationUrl { get; private set; } = string.Empty;
    public string RenderedPayload { get; private set; } = string.Empty;
    public string RenderedHeaders { get; private set; } = "{}";
    public DateTimeOffset RequestedAt { get; private set; }
    public int? ResponseStatusCode { get; private set; }
    public string? ResponseBody { get; private set; }
    public long ResponseTimeMs { get; private set; }
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Guid? ReplayOfDispatchId { get; private set; }
    public WebhookDispatch? ReplayOf { get; private set; }

    public bool IsReplay => ReplayOfDispatchId.HasValue;

    public static WebhookDispatch Create(
        Guid templateId, string destinationUrl, string renderedPayload, string renderedHeaders, DateTimeOffset requestedAt)
        => new(templateId, destinationUrl, renderedPayload, renderedHeaders, requestedAt, null);

    /// <summary>Cria um novo disparo reaproveitando payload/headers/URL do original.</summary>
    public WebhookDispatch CreateReplay(DateTimeOffset requestedAt)
        => new(TemplateId, DestinationUrl, RenderedPayload, RenderedHeaders, requestedAt, Id);

    public void RegisterResponse(int? statusCode, string? responseBody, long responseTimeMs, bool success, string? errorMessage)
    {
        ResponseStatusCode = statusCode;
        ResponseBody = responseBody;
        ResponseTimeMs = responseTimeMs;
        Success = success;
        ErrorMessage = errorMessage;
    }
}
