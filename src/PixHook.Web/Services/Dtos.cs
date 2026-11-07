namespace PixHook.Web.Services;

public sealed record ProviderDto(Guid Id, string Name, string Slug, string Description, bool IsActive);

public sealed record EventTemplateDto(
    Guid Id, Guid ProviderId, string EventType, string Name, string Description,
    string PayloadTemplateJson, string HttpHeadersTemplateJson, string? SignatureStrategyKey,
    List<string> Placeholders);

public sealed record DispatchDto(
    Guid Id, Guid TemplateId, string? TemplateName, string? EventType, Guid? ProviderId, string? ProviderName,
    string DestinationUrl, string RenderedPayload, Dictionary<string, string> RenderedHeaders,
    DateTimeOffset RequestedAt, int? ResponseStatusCode, string? ResponseBody, long ResponseTimeMs,
    bool Success, string? ErrorMessage, Guid? ReplayOfDispatchId);

public sealed record DispatchSummaryDto(
    Guid Id, Guid TemplateId, string? TemplateName, string? EventType, string? ProviderName,
    string DestinationUrl, DateTimeOffset RequestedAt, int? ResponseStatusCode, long ResponseTimeMs,
    bool Success, Guid? ReplayOfDispatchId);

public sealed record PagedResult<T>(List<T> Items, int Page, int PageSize, int TotalCount, int TotalPages,
    bool HasNextPage, bool HasPreviousPage);

public sealed record ScenarioStepDto(
    Guid Id, int Order, Guid TemplateId, string? TemplateName, string? EventType, string? ProviderName,
    int DelayBeforeMs, string Note);

public sealed record ScenarioDto(Guid Id, string Name, string Slug, string Description, bool IsActive,
    List<ScenarioStepDto> Steps);

public sealed record ScenarioStepRunResultDto(int Order, string Note, int DelayAppliedMs, DispatchDto Dispatch);

public sealed record ScenarioRunResultDto(Guid ScenarioId, string ScenarioName, DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt, List<ScenarioStepRunResultDto> Steps);

/// <summary>Resultado de uma chamada à API: ou o valor, ou uma mensagem de erro já pronta pra mostrar.</summary>
public sealed class ApiResult<T>
{
    public T? Value { get; private init; }
    public string? ErrorMessage { get; private init; }
    public bool IsSuccess => ErrorMessage is null;

    public static ApiResult<T> Success(T value) => new() { Value = value };
    public static ApiResult<T> Failure(string message) => new() { ErrorMessage = message };
}
