using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Timeout;
using PixHook.Application.Abstractions;

namespace PixHook.Infrastructure.Dispatching;

/// <summary>Dispatcher HTTP real (HttpClientFactory + Polly: retry/backoff e timeout).</summary>
internal sealed class HttpWebhookDispatcher : IWebhookDispatcher
{
    public const string HttpClientName = "webhook-dispatcher";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DispatcherOptions _options;
    private readonly ILogger<HttpWebhookDispatcher> _logger;

    public HttpWebhookDispatcher(
        IHttpClientFactory httpClientFactory, IOptions<DispatcherOptions> options, ILogger<HttpWebhookDispatcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DispatchResult> SendAsync(
        string url, string payload, IDictionary<string, string> headers, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        foreach (var (name, value) in headers)
        {
            if (string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
            if (!request.Headers.TryAddWithoutValidation(name, value))
                request.Content.Headers.TryAddWithoutValidation(name, value);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            sw.Stop();

            if (body.Length > _options.MaxResponseBodyLength)
                body = body[.._options.MaxResponseBodyLength];

            var status = (int)response.StatusCode;
            return response.IsSuccessStatusCode
                ? DispatchResult.Ok(status, body, sw.ElapsedMilliseconds)
                : DispatchResult.HttpError(status, body, sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (ex is TimeoutRejectedException ||
                                   (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            sw.Stop();
            _logger.LogWarning("Timeout ao disparar webhook para {Url}", url);
            return DispatchResult.Failure($"Timeout: destino não respondeu em {_options.TimeoutSeconds}s.", sw.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Destino indisponível: {Url}", url);
            return DispatchResult.Failure($"Destino indisponível: {ex.Message}", sw.ElapsedMilliseconds);
        }
    }
}
