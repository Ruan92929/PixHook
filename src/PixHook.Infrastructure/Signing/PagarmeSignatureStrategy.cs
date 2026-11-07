using PixHook.Application.Abstractions;

namespace PixHook.Infrastructure.Signing;

/// <summary>
/// Simula o header <c>X-Hub-Signature</c> do Pagar.me. A única assinatura de webhook documentada
/// publicamente pelo Pagar.me (API v1, "Validando um postback") é HMAC-<b>SHA1</b> em hexadecimal,
/// sem prefixo (ao contrário do estilo "sha256=" do GitHub/Stripe). Não há confirmação pública de um
/// esquema equivalente para a API v5 — mantido aqui como a melhor aproximação documentada disponível.
/// </summary>
public sealed class PagarmeSignatureStrategy : ISignatureStrategy
{
    public const string StrategyKey = "pagarme";
    public string Key => StrategyKey;
    public string HeaderName => "X-Hub-Signature";

    public string Sign(string payload, string secret, IReadOnlyDictionary<string, string> headers) =>
        HmacSha256SignatureStrategy.HmacSha1Hex(payload, secret);
}
