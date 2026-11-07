using PixHook.Infrastructure.Signing;

namespace PixHook.Tests.Unit.Infrastructure;

public class SignatureStrategyTests
{
    // Valor de referência: HMAC-SHA256("key", "The quick brown fox jumps over the lazy dog")
    private const string Payload = "The quick brown fox jumps over the lazy dog";
    private const string Expected = "f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8";

    [Fact]
    public void Sign_HmacSha256_RetornaHexMinusculoCorreto()
    {
        new HmacSha256SignatureStrategy().Sign(Payload, "key").Should().Be(Expected);
    }

    [Fact]
    public void Sign_MercadoPago_RetornaFormatoV1()
    {
        var sut = new MercadoPagoSignatureStrategy();

        sut.Sign(Payload, "key").Should().Be($"v1={Expected}");
        sut.HeaderName.Should().Be("x-signature");
    }

    [Fact]
    public void Sign_SegredosDiferentes_GeramAssinaturasDiferentes()
    {
        var sut = new HmacSha256SignatureStrategy();

        sut.Sign("{}", "a").Should().NotBe(sut.Sign("{}", "b"));
    }
}
