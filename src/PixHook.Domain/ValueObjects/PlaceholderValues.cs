using System.Collections.ObjectModel;
using System.Globalization;

namespace PixHook.Domain.ValueObjects;

/// <summary>
/// Value object imutável com os valores que substituem os placeholders de um template.
/// Chaves são case-insensitive.
/// </summary>
public sealed class PlaceholderValues : IEquatable<PlaceholderValues>
{
    private readonly ReadOnlyDictionary<string, string> _values;

    public static PlaceholderValues Empty { get; } = new(new Dictionary<string, string>());

    public PlaceholderValues(IDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase));
    }

    public IReadOnlyDictionary<string, string> Values => _values;

    public bool TryGet(string key, out string value)
    {
        if (_values.TryGetValue(key, out var v)) { value = v; return true; }
        value = string.Empty;
        return false;
    }

    /// <summary>Retorna uma nova instância com o valor definido (imutabilidade).</summary>
    public PlaceholderValues With(string key, string value)
    {
        var copy = new Dictionary<string, string>(_values, StringComparer.OrdinalIgnoreCase) { [key] = value };
        return new PlaceholderValues(copy);
    }

    public PlaceholderValues With(string key, decimal value) =>
        With(key, value.ToString("0.00", CultureInfo.InvariantCulture));

    public PlaceholderValues With(string key, DateTimeOffset value) =>
        With(key, value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture));

    /// <summary>Mescla sobrescrevendo com os valores de <paramref name="overrides"/>.</summary>
    public PlaceholderValues Merge(IDictionary<string, string>? overrides)
    {
        if (overrides is null || overrides.Count == 0) return this;
        var copy = new Dictionary<string, string>(_values, StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in overrides) copy[k] = v;
        return new PlaceholderValues(copy);
    }

    public bool Equals(PlaceholderValues? other) =>
        other is not null && _values.Count == other._values.Count &&
        _values.All(kv => other._values.TryGetValue(kv.Key, out var v) && v == kv.Value);

    public override bool Equals(object? obj) => Equals(obj as PlaceholderValues);

    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var kv in _values)
            hash ^= HashCode.Combine(kv.Key.ToLowerInvariant(), kv.Value);
        return hash;
    }
}
