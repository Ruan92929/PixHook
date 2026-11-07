using PixHook.SampleReceiver.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<WebhookStore>();
builder.Services.AddSingleton<SignatureVerifier>();

var app = builder.Build();
var logger = app.Logger;

app.MapPost("/webhooks/pix", async (HttpRequest request, WebhookStore store, SignatureVerifier verifier) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();

    var headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    var (headerChecked, valid) = verifier.Verify(headers, body);

    var received = new ReceivedWebhook(Guid.NewGuid(), DateTimeOffset.UtcNow, request.Path, headers, body, headerChecked, valid);
    store.Add(received);

    if (valid == false)
        logger.LogWarning("Webhook recebido com assinatura INVÁLIDA ({Header}). Isso quebraria uma integração real.", headerChecked);
    else
        logger.LogInformation("Webhook recebido em {Path} ({Bytes} bytes, assinatura: {Signature})",
            request.Path, body.Length, valid is null ? "não verificada" : "válida");

    // Simula o seu sistema confirmando o recebimento — é isso que o histórico do PixHook registra como sucesso.
    return Results.Ok(new { received = true, id = received.Id });
});

app.MapGet("/webhooks", (WebhookStore store) =>
{
    var items = store.GetAll()
        .OrderByDescending(w => w.ReceivedAt)
        .Select(w => new
        {
            w.Id,
            w.ReceivedAt,
            w.Path,
            w.SignatureHeaderChecked,
            w.SignatureValid,
            w.Headers,
            Body = w.Body.Length > 4000 ? w.Body[..4000] + "…(truncado)" : w.Body
        });
    return Results.Json(items);
});

app.MapDelete("/webhooks", (WebhookStore store) =>
{
    store.Clear();
    return Results.NoContent();
});

app.MapGet("/", () => Results.Content(IndexHtml.Content, "text/html; charset=utf-8"));

app.Run();

/// <summary>Página única, sem build de front-end, que lista os webhooks recebidos e atualiza sozinha.</summary>
static class IndexHtml
{
    public const string Content = """
    <!doctype html>
    <html lang="pt-br">
    <head>
    <meta charset="utf-8" />
    <title>PixHook SampleReceiver</title>
    <style>
      body { font-family: -apple-system, Segoe UI, Arial, sans-serif; margin: 2rem; background: #0b1220; color: #e5e7eb; }
      h1 { font-size: 1.25rem; }
      .sub { color: #9ca3af; margin-bottom: 1.5rem; }
      table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
      th, td { text-align: left; padding: 0.5rem 0.6rem; border-bottom: 1px solid #1f2937; vertical-align: top; }
      th { color: #9ca3af; font-weight: 600; }
      .ok { color: #34d399; } .fail { color: #f87171; } .na { color: #6b7280; }
      pre { white-space: pre-wrap; word-break: break-all; max-width: 520px; margin: 0; font-size: 0.78rem; }
      button { background: #1f2937; color: #e5e7eb; border: 1px solid #374151; border-radius: 6px; padding: 0.4rem 0.8rem; cursor: pointer; }
      button:hover { background: #374151; }
      .empty { color: #6b7280; padding: 2rem 0; }
    </style>
    </head>
    <body>
      <h1>PixHook SampleReceiver</h1>
      <p class="sub">Recebe em <code>POST /webhooks/pix</code> — atualiza sozinho a cada 3s.</p>
      <button onclick="clearAll()">Limpar histórico</button>
      <div id="content" class="empty">Nenhum webhook recebido ainda.</div>
      <script>
        async function clearAll() { await fetch('/webhooks', { method: 'DELETE' }); refresh(); }
        async function refresh() {
          const res = await fetch('/webhooks');
          const items = await res.json();
          const el = document.getElementById('content');
          if (!items.length) { el.className = 'empty'; el.textContent = 'Nenhum webhook recebido ainda.'; return; }
          el.className = '';
          el.innerHTML = '<table><thead><tr><th>Recebido em</th><th>Assinatura</th><th>Headers</th><th>Body</th></tr></thead><tbody>' +
            items.map(w => {
              const sig = w.signatureValid === true ? '<span class="ok">válida</span>'
                        : w.signatureValid === false ? '<span class="fail">inválida (' + w.signatureHeaderChecked + ')</span>'
                        : '<span class="na">não assinado</span>';
              const headers = Object.entries(w.headers).map(([k,v]) => k + ': ' + v).join('\n');
              return '<tr><td>' + new Date(w.receivedAt).toLocaleTimeString() + '</td><td>' + sig +
                     '</td><td><pre>' + escapeHtml(headers) + '</pre></td><td><pre>' + escapeHtml(w.body) + '</pre></td></tr>';
            }).join('') + '</tbody></table>';
        }
        function escapeHtml(s) { return s.replace(/[&<>]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;'})[c]); }
        refresh();
        setInterval(refresh, 3000);
      </script>
    </body>
    </html>
    """;
}
