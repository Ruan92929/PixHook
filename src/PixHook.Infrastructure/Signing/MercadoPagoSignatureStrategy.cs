using System.Globalization;
using System.Text.Json;
using PixHook.Application.Abstractions;

namespace PixHook.Infrastructure.Signing;

/// <summary>
/// Simula o header <c>x-signature</c> do Mercado Pago. A MP não assina o corpo cru: ela monta um
/// "manifest" de texto <c>id:{data.id};request-id:{x-request-id};ts:{ts};</c> (data.id em minúsculo)
/// e assina ESSE manifest em HMAC-SHA256, publicando o resultado como <c>ts=&lt;ts&gt;,v1=&lt;hex&gt;</c>.
/// Confirmado na doc oficial (Checkout API v2 / Webhooks) e na implementação de referência dos SDKs.
/// </summary>
public sealed class MercadoPagoSignatureStrategy : ISignatureStrategy
{
    public const string StrategyKey = "mercadopago";
    public string Key => StrategyKey;
    public string HeaderName => "x-signature";

    public string Sign(string payload, string secret, IReadOnlyDictionary<string, string> headers)
    {
        var dataId = ExtractDataId(payload);
        headers.TryGetValue("x-request-id", out var requestId);
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
        var hex = HmacSha256SignatureStrategy.HmacHex(manifest, secret);
        return $"ts={ts},v1={hex}";
    }

    private static string ExtractDataId(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var id))
                return (id.GetString() ?? id.ToString()).ToLowerInvariant();
        }
        catch (JsonException)
        {
            // Payload customizado sem o shape esperado: assina com id vazio em vez de falhar o disparo.
        }

        return string.Empty;
    }
}
