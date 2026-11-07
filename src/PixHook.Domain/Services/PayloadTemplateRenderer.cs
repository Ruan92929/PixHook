using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using PixHook.Domain.ValueObjects;

namespace PixHook.Domain.Services;

/// <summary>
/// Regra de negócio pura: substitui placeholders <c>{{nome}}</c> do template pelos valores informados.
/// Placeholders sem valor permanecem intactos e são reportados em <see cref="RenderResult.MissingPlaceholders"/>.
/// Valores são escapados para JSON (aspas, barras, quebras de linha).
/// </summary>
public static partial class PayloadTemplateRenderer
{
    [GeneratedRegex(@"\{\{\s*([a-zA-Z0-9_\.]+)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegex();

    public static RenderResult Render(string template, PlaceholderValues values, bool jsonEscape = true)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);

        var missing = new List<string>();
        var output = PlaceholderRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            if (!values.TryGet(key, out var value))
            {
                if (!missing.Contains(key, StringComparer.OrdinalIgnoreCase)) missing.Add(key);
                return match.Value;
            }
            return jsonEscape ? JavaScriptEncoder.UnsafeRelaxedJsonEscaping.Encode(value) : value;
        });

        return new RenderResult(output, missing);
    }

    public static IReadOnlyList<string> ExtractPlaceholders(string template) =>
        PlaceholderRegex().Matches(template ?? string.Empty)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

public sealed record RenderResult(string Content, IReadOnlyList<string> MissingPlaceholders)
{
    public bool HasMissingPlaceholders => MissingPlaceholders.Count > 0;
}
