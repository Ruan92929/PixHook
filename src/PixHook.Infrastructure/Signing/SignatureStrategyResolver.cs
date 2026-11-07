using Microsoft.Extensions.Configuration;
using PixHook.Application.Abstractions;

namespace PixHook.Infrastructure.Signing;

internal sealed class SignatureStrategyResolver : ISignatureStrategyResolver
{
    private readonly Dictionary<string, ISignatureStrategy> _strategies;

    public SignatureStrategyResolver(IEnumerable<ISignatureStrategy> strategies) =>
        _strategies = strategies.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);

    public ISignatureStrategy? Resolve(string? key) =>
        key is not null && _strategies.TryGetValue(key, out var s) ? s : null;
}

/// <summary>Lê segredos de <c>Signing:Secrets:{chave}</c>, com fallback em <c>Signing:DefaultSecret</c>.</summary>
internal sealed class ConfigurationWebhookSecretProvider : IWebhookSecretProvider
{
    private readonly IConfiguration _configuration;

    public ConfigurationWebhookSecretProvider(IConfiguration configuration) => _configuration = configuration;

    public string GetSecret(string strategyKey) =>
        _configuration[$"Signing:Secrets:{strategyKey}"]
        ?? _configuration["Signing:DefaultSecret"]
        ?? "pixhook-dev-secret";
}
