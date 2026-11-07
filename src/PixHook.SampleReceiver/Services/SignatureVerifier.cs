using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PixHook.SampleReceiver.Services;

/// <summary>
/// Confere a assinatura dos provedores que assinam o payload (Mercado Pago e Pagar.me).
/// Os segredos aqui têm que ser os mesmos configurados no PixHook (appsettings → Signing:Secrets),
/// já que num cenário real cada provedor combina o segredo com você no painel dele.
///
/// Mercado Pago: NÃO assina o corpo cru. Assina o manifest de texto
/// "id:{data.id};request-id:{x-request-id};ts:{ts};" (data.id em minúsculo) em HMAC-SHA256, publicado
/// como "ts=&lt;ts&gt;,v1=&lt;hex&gt;" no header x-signature.
/// Pagar.me: a única assinatura documentada publicamente (API v1, "Validando um postback") é
/// HMAC-SHA1 em hex, sem prefixo, no header X-Hub-Signature.
/// </summary>
public sealed class SignatureVerifier
{
    private readonly IConfiguration _configuration;

    public SignatureVerifier(IConfiguration configuration) => _configuration = configuration;

    /// <summary>Tenta validar a assinatura usando o header que o provedor efetivamente mandou.</summary>
    public (string? HeaderChecked, bool? Valid) Verify(IDictionary<string, string> headers, string body)
    {
        if (headers.TryGetValue("x-signature", out var mpSignature))
            return ("x-signature", ValidateMercadoPago(mpSignature, body, headers));

        if (headers.TryGetValue("X-Hub-Signature", out var pagarmeSignature))
            return ("X-Hub-Signature", ValidatePagarme(pagarmeSignature, body));

        if (headers.TryGetValue("X-Signature", out var genericSignature))
            return ("X-Signature", ValidateGeneric(genericSignature, body));

        return (null, null); // provedor não assina (ex.: Efi, PagBank, Cielo) — nada a validar.
    }

    private bool ValidateMercadoPago(string headerValue, string body, IDictionary<string, string> headers)
    {
        // Formato real: "ts=<timestamp>,v1=<hex>".
        var parts = headerValue.Split(',')
            .Select(p => p.Split('=', 2))
            .Where(kv => kv.Length == 2)
            .ToDictionary(kv => kv[0].Trim(), kv => kv[1].Trim());

        if (!parts.TryGetValue("ts", out var ts) || !parts.TryGetValue("v1", out var v1))
            return false;

        var dataId = ExtractDataId(body);
        headers.TryGetValue("x-request-id", out var requestId);

        var manifest = $"id:{dataId};request-id:{requestId};ts:{ts};";
        var expectedHex = ComputeHex(manifest, GetSecret("mercadopago"));
        return FixedTimeEquals(v1, expectedHex);
    }

    private static string ExtractDataId(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var id))
                return (id.GetString() ?? id.ToString()).ToLowerInvariant();
        }
        catch (JsonException)
        {
            // Corpo customizado sem o shape esperado: valida com id vazio (vai falhar, o que é o esperado).
        }

        return string.Empty;
    }

    private bool ValidatePagarme(string headerValue, string body) =>
        FixedTimeEquals(headerValue, ComputeHexSha1(body, GetSecret("pagarme")));

    private bool ValidateGeneric(string headerValue, string body) =>
        FixedTimeEquals(headerValue, ComputeHex(body, GetSecret("hmac-sha256")));

    private string GetSecret(string key) =>
        _configuration[$"Signing:Secrets:{key}"] ?? _configuration["Signing:DefaultSecret"] ?? "pixhook-dev-secret";

    private static string ComputeHex(string payload, string secret)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ComputeHexSha1(string payload, string secret)
    {
        var hash = HMACSHA1.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a.ToLowerInvariant()), Encoding.UTF8.GetBytes(b.ToLowerInvariant()));
}
