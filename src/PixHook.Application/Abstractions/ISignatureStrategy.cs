namespace PixHook.Application.Abstractions;

/// <summary>
/// Strategy de assinatura de webhook (HMAC ou outro esquema) específica de cada provedor.
/// Ponto de extensão: novos esquemas = nova implementação registrada no DI.
/// </summary>
public interface ISignatureStrategy
{
    /// <summary>Chave referenciada por <c>WebhookEventTemplate.SignatureStrategyKey</c>.</summary>
    string Key { get; }

    /// <summary>Nome do header HTTP onde a assinatura é enviada.</summary>
    string HeaderName { get; }

    /// <summary>
    /// Calcula o valor do header de assinatura. <paramref name="headers"/> traz os demais headers já
    /// renderizados do template (ex.: x-request-id), necessários para esquemas cujo "manifest" assinado
    /// não é o corpo cru (caso do Mercado Pago).
    /// </summary>
    string Sign(string payload, string secret, IReadOnlyDictionary<string, string> headers);
}

public interface ISignatureStrategyResolver
{
    ISignatureStrategy? Resolve(string? key);
}

/// <summary>Fornece o segredo usado para assinar webhooks de cada estratégia.</summary>
public interface IWebhookSecretProvider
{
    string GetSecret(string strategyKey);
}
