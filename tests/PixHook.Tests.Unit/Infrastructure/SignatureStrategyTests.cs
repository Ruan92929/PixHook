using PixHook.Infrastructure.Signing;

namespace PixHook.Tests.Unit.Infrastructure;

public class SignatureStrategyTests
{
    // Valor de referência: HMAC-SHA256("key", "The quick brown fox jumps over the lazy dog")
    private const string Payload = "The quick brown fox jumps over the lazy dog";
    private const string Expected = "f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8";
    private static readonly IReadOnlyDictionary<string, string> NoHeaders = new Dictionary<string, string>();

    [Fact]
    public void Sign_HmacSha256_RetornaHexMinusculoCorreto()
    {
        new HmacSha256SignatureStrategy().Sign(Payload, "key", NoHeaders).Should().Be(Expected);
    }

    [Fact]
    public void Sign_MercadoPago_AssinaManifestComDataIdRequestIdETs()
    {
        // A MP não assina o corpo cru: monta "id:{data.id};request-id:{x-request-id};ts:{ts};" e assina
        // esse manifest. Por isso o teste recalcula o hash esperado a partir do próprio ts gerado.
        var sut = new MercadoPagoSignatureStrategy();
        const string payload = """{"data":{"id":"123456789"}}""";
        var headers = new Dictionary<string, string> { ["x-request-id"] = "req-1" };

        var header = sut.Sign(payload, "key", headers);

        header.Should().MatchRegex(@"^ts=\d+,v1=[0-9a-f]{64}$");
        sut.HeaderName.Should().Be("x-signature");

        var ts = header.Split(',')[0]["ts=".Length..];
        var expectedManifest = $"id:123456789;request-id:req-1;ts:{ts};";
        var expectedHex = HmacSha256SignatureStrategy.HmacHex(expectedManifest, "key");
        header.Should().Be($"ts={ts},v1={expectedHex}");
    }

    [Fact]
    public void Sign_Pagarme_RetornaHmacSha1SemPrefixo()
    {
        // Única assinatura documentada publicamente pelo Pagar.me (API v1): HMAC-SHA1 hex, sem "sha256=".
        var sut = new PagarmeSignatureStrategy();

        var header = sut.Sign(Payload, "key", NoHeaders);

        header.Should().MatchRegex("^[0-9a-f]{40}$");
        header.Should().Be(HmacSha256SignatureStrategy.HmacSha1Hex(Payload, "key"));
        sut.HeaderName.Should().Be("X-Hub-Signature");
    }

    [Fact]
    public void Sign_SegredosDiferentes_GeramAssinaturasDiferentes()
    {
        var sut = new HmacSha256SignatureStrategy();

        sut.Sign("{}", "a", NoHeaders).Should().NotBe(sut.Sign("{}", "b", NoHeaders));
    }
}
