using System.Collections.Concurrent;

namespace PixHook.SampleReceiver.Services;

/// <summary>Um webhook recebido, guardado em memória só para esta execução (reinicia zerado).</summary>
public sealed record ReceivedWebhook(
    Guid Id,
    DateTimeOffset ReceivedAt,
    string Path,
    IReadOnlyDictionary<string, string> Headers,
    string Body,
    string? SignatureHeaderChecked,
    bool? SignatureValid);

/// <summary>
/// Guarda os últimos webhooks recebidos em memória (nada de banco aqui — é só um exemplo de
/// "sistema do lojista" para você ver e testar os webhooks que o PixHook dispara).
/// </summary>
public sealed class WebhookStore
{
    private const int MaxItems = 200;
    private readonly ConcurrentStack<ReceivedWebhook> _items = new();

    public void Add(ReceivedWebhook webhook)
    {
        _items.Push(webhook);
        // Poda ocasional para não crescer sem limite numa sessão longa de testes.
        if (_items.Count > MaxItems && _items.TryPeek(out _))
        {
            var kept = _items.Take(MaxItems).ToList();
            _items.Clear();
            foreach (var item in kept.AsEnumerable().Reverse()) _items.Push(item);
        }
    }

    public IReadOnlyList<ReceivedWebhook> GetAll() => _items.ToList();

    public void Clear() => _items.Clear();
}
