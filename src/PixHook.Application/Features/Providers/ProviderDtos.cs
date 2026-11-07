using PixHook.Domain.Entities;
using PixHook.Domain.Enums;

namespace PixHook.Application.Features.Providers;

/// <summary>Provedor de pagamento simulado.</summary>
public sealed record ProviderDto(Guid Id, string Name, string Slug, string Description, bool IsActive)
{
    public static ProviderDto From(WebhookProvider p) => new(p.Id, p.Name, p.Slug, p.Description, p.IsActive);
}

/// <summary>Template de evento disparável de um provedor.</summary>
public sealed record EventTemplateDto(
    Guid Id,
    Guid ProviderId,
    EventType EventType,
    string Name,
    string Description,
    string PayloadTemplateJson,
    string HttpHeadersTemplateJson,
    string? SignatureStrategyKey,
    IReadOnlyList<string> Placeholders)
{
    public static EventTemplateDto From(WebhookEventTemplate t) => new(
        t.Id, t.ProviderId, t.EventType, t.Name, t.Description,
        t.PayloadTemplateJson, t.HttpHeadersTemplateJson, t.SignatureStrategyKey,
        Domain.Services.PayloadTemplateRenderer.ExtractPlaceholders(t.PayloadTemplateJson));
}
