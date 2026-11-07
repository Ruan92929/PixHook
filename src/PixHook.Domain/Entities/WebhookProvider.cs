namespace PixHook.Domain.Entities;

/// <summary>Agregado raiz: provedor de pagamento simulado (Mercado Pago, Efi, ...).</summary>
public class WebhookProvider
{
    private readonly List<WebhookEventTemplate> _templates = new();

    private WebhookProvider() { } // EF Core

    public WebhookProvider(Guid id, string name, string slug, string description, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nome é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug é obrigatório.", nameof(slug));

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description ?? string.Empty;
        IsActive = isActive;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<WebhookEventTemplate> Templates => _templates.AsReadOnly();

    public WebhookEventTemplate AddTemplate(WebhookEventTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (template.ProviderId != Id)
            throw new InvalidOperationException("Template pertence a outro provedor.");
        _templates.Add(template);
        return template;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
