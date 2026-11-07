using System.Text.Json;
using PixHook.Application.Abstractions;
using PixHook.Application.Common;
using PixHook.Domain.Entities;
using PixHook.Domain.Services;
using PixHook.Domain.ValueObjects;

namespace PixHook.Application.Services;

public sealed record BuiltWebhookRequest(string Payload, IDictionary<string, string> Headers);

/// <summary>
/// Monta payload + headers finais a partir do template: renderiza placeholders, valida o JSON,
/// mescla headers customizados e aplica a assinatura (Strategy) quando configurada.
/// </summary>
public interface IWebhookRequestBuilder
{
    Result<BuiltWebhookRequest> Build(
        WebhookEventTemplate template,
        IDictionary<string, string>? payloadOverrides,
        IDictionary<string, string>? customHeaders);
}

public sealed class WebhookRequestBuilder : IWebhookRequestBuilder
{
    private readonly IPlaceholderFactory _placeholderFactory;
    private readonly ISignatureStrategyResolver _signatureResolver;
    private readonly IWebhookSecretProvider _secretProvider;

    public WebhookRequestBuilder(
        IPlaceholderFactory placeholderFactory,
        ISignatureStrategyResolver signatureResolver,
        IWebhookSecretProvider secretProvider)
    {
        _placeholderFactory = placeholderFactory;
        _signatureResolver = signatureResolver;
        _secretProvider = secretProvider;
    }

    public Result<BuiltWebhookRequest> Build(
        WebhookEventTemplate template,
        IDictionary<string, string>? payloadOverrides,
        IDictionary<string, string>? customHeaders)
    {
        var values = _placeholderFactory.CreateDefaults().Merge(payloadOverrides);

        var payload = PayloadTemplateRenderer.Render(template.PayloadTemplateJson, values);
        if (payload.HasMissingPlaceholders)
        {
            return Error.Unprocessable("payload.missing_placeholders",
                $"Placeholders sem valor: {string.Join(", ", payload.MissingPlaceholders)}. Informe-os em payloadOverrides.",
                new Dictionary<string, string[]> { ["payloadOverrides"] = payload.MissingPlaceholders.ToArray() });
        }

        if (!IsValidJson(payload.Content, out var jsonError))
        {
            return Error.Unprocessable("payload.invalid_json",
                $"O payload renderizado não é um JSON válido: {jsonError}");
        }

        var headersRendered = PayloadTemplateRenderer.Render(template.HttpHeadersTemplateJson, values);
        Dictionary<string, string> headers;
        try
        {
            headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersRendered.Content)
                      ?? new Dictionary<string, string>();
        }
        catch (JsonException ex)
        {
            return Error.Unprocessable("headers.invalid_json", $"Template de headers inválido: {ex.Message}");
        }

        headers = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
        if (customHeaders is not null)
            foreach (var (k, v) in customHeaders) headers[k] = v;

        if (template.SignatureStrategyKey is not null)
        {
            var strategy = _signatureResolver.Resolve(template.SignatureStrategyKey);
            if (strategy is null)
                return Error.Failure("signature.strategy_not_found",
                    $"Estratégia de assinatura '{template.SignatureStrategyKey}' não registrada.");

            var secret = _secretProvider.GetSecret(strategy.Key);
            headers[strategy.HeaderName] = strategy.Sign(payload.Content, secret, headers);
        }

        return new BuiltWebhookRequest(payload.Content, headers);
    }

    private static bool IsValidJson(string content, out string? error)
    {
        try
        {
            using var _ = JsonDocument.Parse(content);
            error = null;
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
