using PixHook.Domain.ValueObjects;

namespace PixHook.Tests.Unit.Domain;

public class PlaceholderValuesTests
{
    [Fact]
    public void With_NovoValor_NaoAlteraInstanciaOriginal()
    {
        var original = PlaceholderValues.Empty;

        var updated = original.With("txid", "1");

        original.Values.Should().BeEmpty();
        updated.Values.Should().ContainKey("txid");
    }

    [Fact]
    public void Merge_ComOverrides_SobrescreveValoresExistentes()
    {
        var values = PlaceholderValues.Empty.With("valor", "1.00").With("txid", "a");

        var merged = values.Merge(new Dictionary<string, string> { ["VALOR"] = "9.99" });

        merged.TryGet("valor", out var valor).Should().BeTrue();
        valor.Should().Be("9.99");
        merged.TryGet("txid", out _).Should().BeTrue();
    }

    [Fact]
    public void Equals_MesmosValores_SaoIguais()
    {
        var a = PlaceholderValues.Empty.With("x", "1");
        var b = PlaceholderValues.Empty.With("X", "1");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
