using System.Text.Json;
using PixHook.Domain.Entities;
using PixHook.Domain.Enums;

namespace PixHook.Application.Features.Dispatches;

/// <summary>Detalhe completo de um disparo de webhook.</summary>
public sealed record DispatchDto(
    Guid Id,
    Guid TemplateId,
    string? TemplateName,
    EventType? EventType,
    Guid? ProviderId,
    string? ProviderName,
    string DestinationUrl,
    string RenderedPayload,
    IReadOnlyDictionary<string, string> RenderedHeaders,
    DateTimeOffset RequestedAt,
    int? ResponseStatusCode,
    string? ResponseBody,
    long ResponseTimeMs,
    bool Success,
    string? ErrorMessage,
    Guid? ReplayOfDispatchId)
{
    public static DispatchDto From(WebhookDispatch d) => new(
        d.Id, d.TemplateId, d.Template?.Name, d.Template?.EventType,
        d.Template?.ProviderId, d.Template?.Provider?.Name,
        d.DestinationUrl, d.RenderedPayload, DeserializeHeaders(d.RenderedHeaders),
        d.RequestedAt, d.ResponseStatusCode, d.ResponseBody, d.ResponseTimeMs,
        d.Success, d.ErrorMessage, d.ReplayOfDispatchId);

    internal static IReadOnlyDictionary<string, string> DeserializeHeaders(string json)
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new(); }
        catch (JsonException) { return new Dictionary<string, string>(); }
    }
}

/// <summary>Item resumido do histórico de disparos.</summary>
public sealed record DispatchSummaryDto(
    Guid Id,
    Guid TemplateId,
    string? TemplateName,
    EventType? EventType,
    string? ProviderName,
    string DestinationUrl,
    DateTimeOffset RequestedAt,
    int? ResponseStatusCode,
    long ResponseTimeMs,
    bool Success,
    Guid? ReplayOfDispatchId)
{
    public static DispatchSummaryDto From(WebhookDispatch d) => new(
        d.Id, d.TemplateId, d.Template?.Name, d.Template?.EventType, d.Template?.Provider?.Name,
        d.DestinationUrl, d.RequestedAt, d.ResponseStatusCode, d.ResponseTimeMs, d.Success, d.ReplayOfDispatchId);
}
