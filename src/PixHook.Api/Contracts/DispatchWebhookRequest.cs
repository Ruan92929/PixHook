namespace PixHook.Api.Contracts;

/// <summary>Corpo para disparar um webhook simulado.</summary>
public sealed class DispatchWebhookRequest
{
    /// <summary>Id do template de evento (veja GET /api/v1/providers/{id}/events).</summary>
    public Guid TemplateId { get; init; }

    /// <summary>URL que receberá o webhook (ex.: http://localhost:5000/webhooks/pix).</summary>
    public string DestinationUrl { get; init; } = string.Empty;

    /// <summary>Valores que sobrescrevem os placeholders do template (ex.: valor, txid).</summary>
    public Dictionary<string, string>? PayloadOverrides { get; init; }

    /// <summary>Headers HTTP adicionais (sobrescrevem os do template).</summary>
    public Dictionary<string, string>? CustomHeaders { get; init; }
}
