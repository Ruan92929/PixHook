using System.Globalization;
using PixHook.Application.Abstractions;
using PixHook.Domain.ValueObjects;

namespace PixHook.Application.Services;

/// <summary>Gera valores padrão realistas para os placeholders mais comuns de PIX.</summary>
public interface IPlaceholderFactory
{
    PlaceholderValues CreateDefaults();
}

public sealed class DefaultPlaceholderFactory : IPlaceholderFactory
{
    private const string Alphanumeric = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private readonly IClock _clock;

    public DefaultPlaceholderFactory(IClock clock) => _clock = clock;

    public PlaceholderValues CreateDefaults()
    {
        var now = _clock.UtcNow;
        var txid = RandomString(32);
        return PlaceholderValues.Empty
            .With("txid", txid)
            .With("valor", 150.00m)
            .With("valorOriginal", 150.00m)
            .With("valorPago", 149.90m)
            .With("dataHora", now)
            .With("dataExpiracao", now.AddHours(-1))
            .With("endToEndId", $"E{RandomDigits(8)}{now:yyyyMMddHHmm}{RandomString(11)}")
            .With("chave", "pagamentos@lojaexemplo.com.br")
            .With("pagadorNome", "Fulano de Tal")
            .With("pagadorCpf", "12345678909")
            .With("paymentId", RandomDigits(11))
            .With("externalReference", $"pedido-{RandomDigits(6)}")
            .With("requestId", Guid.NewGuid().ToString())
            .With("timestamp", now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
    }

    private static string RandomString(int length) =>
        string.Create(length, 0, (span, _) =>
        {
            for (var i = 0; i < span.Length; i++) span[i] = Alphanumeric[Random.Shared.Next(Alphanumeric.Length)];
        });

    private static string RandomDigits(int length) =>
        string.Create(length, 0, (span, _) =>
        {
            for (var i = 0; i < span.Length; i++) span[i] = (char)('0' + Random.Shared.Next(10));
        });
}
