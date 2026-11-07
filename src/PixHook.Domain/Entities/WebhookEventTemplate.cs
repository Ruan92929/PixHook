using PixHook.Domain.Enums;

namespace PixHook.Domain.Entities;

/// <summary>
/// Tipo de evento disparável para um provedor. O payload e os headers são DADOS
/// (JSON com placeholders como {{valor}}, {{txid}}, {{dataHora}}), nunca código.
/// </summary>
public class WebhookEventTemplate
{
    private WebhookEventTemplate() { } // EF Core

    public WebhookEventTemplate(
        Guid id,
        Guid providerId,
        EventType eventType,
        string name,
        string description,
        string payloadTemplateJson,
        string httpHeadersTemplateJson,
        string? signatureStrategyKey = null)
    {
        if (providerId == Guid.Empty) throw new ArgumentException("ProviderId é obrigatório.", nameof(providerId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nome é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(payloadTemplateJson)) throw new ArgumentException("Payload é obrigatório.", nameof(payloadTemplateJson));

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        ProviderId = providerId;
        EventType = eventType;
        Name = name;
        Description = description ?? string.Empty;
        PayloadTemplateJson = payloadTemplateJson;
        HttpHeadersTemplateJson = string.IsNullOrWhiteSpace(httpHeadersTemplateJson) ? "{}" : httpHeadersTemplateJson;
        SignatureStrategyKey = string.IsNullOrWhiteSpace(signatureStrategyKey) ? null : signatureStrategyKey;
    }

    public Guid Id { get; private set; }
    public Guid ProviderId { get; private set; }
    public WebhookProvider? Provider { get; private set; }
    public EventType EventType { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string PayloadTemplateJson { get; private set; } = "{}";
    public string HttpHeadersTemplateJson { get; private set; } = "{}";
    public string? SignatureStrategyKey { get; private set; }
}
