using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace PixHook.Web.Services;

/// <summary>
/// Cliente fino para a PixHook.Api: nenhuma lógica de negócio aqui, só monta as chamadas HTTP e
/// traduz erros (ProblemDetails) em mensagens prontas para a tela.
/// </summary>
public sealed class PixHookApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public PixHookApiClient(HttpClient http) => _http = http;

    public async Task<List<ProviderDto>> GetProvidersAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<ProviderDto>>("api/v1/providers", JsonOptions, ct) ?? new();

    public async Task<List<EventTemplateDto>> GetProviderEventsAsync(Guid providerId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<EventTemplateDto>>($"api/v1/providers/{providerId}/events", JsonOptions, ct) ?? new();

    public async Task<PagedResult<DispatchSummaryDto>> GetHistoryAsync(int page, int pageSize, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<PagedResult<DispatchSummaryDto>>(
            $"api/v1/dispatches?page={page}&pageSize={pageSize}", JsonOptions, ct)
        ?? new PagedResult<DispatchSummaryDto>(new(), page, pageSize, 0, 0, false, false);

    public async Task<ApiResult<DispatchDto>> DispatchAsync(
        Guid templateId, string destinationUrl, Dictionary<string, string>? overrides,
        Dictionary<string, string>? headers, CancellationToken ct = default)
    {
        var body = new { templateId, destinationUrl, payloadOverrides = overrides, customHeaders = headers };
        var response = await _http.PostAsJsonAsync("api/v1/dispatches", body, JsonOptions, ct);
        return await ReadResultAsync<DispatchDto>(response, ct);
    }

    public async Task<ApiResult<DispatchDto>> ReplayAsync(Guid dispatchId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"api/v1/dispatches/{dispatchId}/replay", content: null, ct);
        return await ReadResultAsync<DispatchDto>(response, ct);
    }

    public async Task<List<ScenarioDto>> GetScenariosAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<ScenarioDto>>("api/v1/scenarios", JsonOptions, ct) ?? new();

    public async Task<ApiResult<ScenarioRunResultDto>> RunScenarioAsync(
        Guid scenarioId, string destinationUrl, CancellationToken ct = default)
    {
        var body = new { destinationUrl };
        var response = await _http.PostAsJsonAsync($"api/v1/scenarios/{scenarioId}/run", body, JsonOptions, ct);
        return await ReadResultAsync<ScenarioRunResultDto>(response, ct);
    }

    private static async Task<ApiResult<T>> ReadResultAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
            return value is not null ? ApiResult<T>.Success(value) : ApiResult<T>.Failure("A API respondeu sem conteúdo.");
        }

        return ApiResult<T>.Failure(await DescribeErrorAsync(response, ct));
    }

    private static async Task<string> DescribeErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = doc.RootElement;
            var detail = root.TryGetProperty("detail", out var d) ? d.GetString() : null;
            var title = root.TryGetProperty("title", out var t) ? t.GetString() : null;

            var sb = new StringBuilder(detail ?? title ?? $"A API respondeu {(int)response.StatusCode}.");
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var field in errors.EnumerateObject())
                {
                    var messages = field.Value.EnumerateArray().Select(m => m.GetString());
                    sb.Append($" [{field.Name}: {string.Join("; ", messages)}]");
                }
            }
            return sb.ToString();
        }
        catch (JsonException)
        {
            return $"A API respondeu {(int)response.StatusCode} ({response.ReasonPhrase}).";
        }
    }
}
