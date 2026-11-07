using PixHook.Domain.Entities;

namespace PixHook.Tests.Unit.Domain;

public class WebhookDispatchTests
{
    [Fact]
    public void CreateReplay_DisparoExistente_CopiaPayloadEReferenciaOriginal()
    {
        var original = WebhookDispatch.Create(Guid.NewGuid(), "http://localhost/x", "{\"a\":1}", "{\"h\":\"v\"}", DateTimeOffset.UtcNow);

        var replay = original.CreateReplay(DateTimeOffset.UtcNow.AddMinutes(1));

        replay.Id.Should().NotBe(original.Id);
        replay.ReplayOfDispatchId.Should().Be(original.Id);
        replay.RenderedPayload.Should().Be(original.RenderedPayload);
        replay.RenderedHeaders.Should().Be(original.RenderedHeaders);
        replay.DestinationUrl.Should().Be(original.DestinationUrl);
        replay.IsReplay.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ProviderSemNome_LancaArgumentException()
    {
        var act = () => new WebhookProvider(Guid.NewGuid(), " ", "slug", "d");

        act.Should().Throw<ArgumentException>();
    }
}
