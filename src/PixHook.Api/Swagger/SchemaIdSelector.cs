namespace PixHook.Api.Swagger;

/// <summary>Nomes de schema legíveis: remove sufixo "Dto" e expande genéricos (PagedResult&lt;X&gt; → PagedDispatchSummary).</summary>
internal static class SchemaIdSelector
{
    public static string GetSchemaId(Type type)
    {
        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            var args = string.Concat(type.GetGenericArguments().Select(GetSchemaId));
            return name switch
            {
                "PagedResult" => $"Paged{args}",
                "Dictionary" or "IDictionary" or "IReadOnlyDictionary" => $"MapOf{args}",
                _ => $"{name}Of{args}"
            };
        }

        var simple = type.Name;
        return simple.EndsWith("Dto", StringComparison.Ordinal) ? simple[..^3] : simple;
    }
}
