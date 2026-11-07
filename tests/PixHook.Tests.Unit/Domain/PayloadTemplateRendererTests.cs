using PixHook.Domain.Services;
using PixHook.Domain.ValueObjects;

namespace PixHook.Tests.Unit.Domain;

public class PayloadTemplateRendererTests
{
    [Fact]
    public void Render_TodosPlaceholdersInformados_SubstituiValores()
    {
        var values = PlaceholderValues.Empty.With("txid", "abc123").With("valor", "10.00");

        var result = PayloadTemplateRenderer.Render("""{"txid":"{{txid}}","valor":"{{ valor }}"}""", values);

        result.Content.Should().Be("""{"txid":"abc123","valor":"10.00"}""");
        result.HasMissingPlaceholders.Should().BeFalse();
    }

    [Fact]
    public void Render_PlaceholderAusente_MantemPlaceholderEReportaAusencia()
    {
        var result = PayloadTemplateRenderer.Render("""{"txid":"{{txid}}","e2e":"{{endToEndId}}"}""",
            PlaceholderValues.Empty.With("txid", "x"));

        result.Content.Should().Contain("{{endToEndId}}");
        result.MissingPlaceholders.Should().ContainSingle().Which.Should().Be("endToEndId");
    }

    [Fact]
    public void Render_ChaveComCaixaDiferente_SubstituiIgnorandoCaixa()
    {
        var result = PayloadTemplateRenderer.Render("{{TXID}}", PlaceholderValues.Empty.With("txid", "ok"));

        result.Content.Should().Be("ok");
    }

    [Fact]
    public void Render_ValorDecimal_FormataComPontoEDuasCasas()
    {
        var values = PlaceholderValues.Empty.With("valor", 1234.5m);

        var result = PayloadTemplateRenderer.Render("""{"valor":{{valor}}}""", values);

        result.Content.Should().Be("""{"valor":1234.50}""");
    }

    [Fact]
    public void Render_ValorData_FormataIso8601ComOffset()
    {
        var date = new DateTimeOffset(2026, 11, 7, 14, 30, 5, 123, TimeSpan.FromHours(-3));

        var result = PayloadTemplateRenderer.Render("{{dataHora}}", PlaceholderValues.Empty.With("dataHora", date));

        result.Content.Should().Be("2026-11-07T14:30:05.123-03:00");
    }

    [Fact]
    public void Render_ValorComAspas_EscapaParaJson()
    {
        var result = PayloadTemplateRenderer.Render("""{"nome":"{{nome}}"}""",
            PlaceholderValues.Empty.With("nome", "João \"Zé\""));

        result.Content.Should().Be("""{"nome":"João \"Zé\""}""");
    }

    [Fact]
    public void ExtractPlaceholders_TemplateComRepetidos_RetornaDistintos()
    {
        var placeholders = PayloadTemplateRenderer.ExtractPlaceholders("{{a}} {{b}} {{a}}");

        placeholders.Should().BeEquivalentTo(new[] { "a", "b" });
    }
}
