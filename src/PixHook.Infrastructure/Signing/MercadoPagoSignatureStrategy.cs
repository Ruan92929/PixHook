using PixHook.Application.Abstractions;

namespace PixHook.Infrastructure.Signing;

/// <summary>
/// Simula o header <c>x-signature</c> do Mercado Pago (formato <c>v1=&lt;hmac-sha256-hex&gt;</c>)
/// assinando o corpo do webhook.
/// </summary>
public sealed class MercadoPagoSignatureStrategy : ISignatureStrategy
{
    public const string StrategyKey = "mercadopago";
    public string Key => StrategyKey;
    public string HeaderName => "x-signature";

    public string Sign(string payload, string secret) => $"v1={HmacSha256SignatureStrategy.HmacHex(payload, secret)}";
}
