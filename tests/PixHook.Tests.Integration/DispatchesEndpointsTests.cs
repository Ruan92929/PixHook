using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PixHook.Application.Common;
using PixHook.Application.Features.Dispatches;
using PixHook.Infrastructure.Persistence.Seed;
using PixHook.Infrastructure.Signing;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace PixHook.Tests.Integration;

[Collection(ApiCollection.Name)]
public class DispatchesEndpointsTests : ApiTestBase
{
    public DispatchesEndpointsTests(PixHookApiFactory factory) : base(factory) { }

    private string DestinationUrl(string path) => $"{Factory.Destination.Urls[0]}{path}";

    private Task<HttpResponseMessage> PostDispatch(object body) => Client.PostAsJsonAsync("/api/v1/dispatches", body);

    [Fact]
    public async Task PostDispatch_DestinoResponde200_EntregaPayloadEHeadersEsperados()
    {
        var path = $"/webhooks/{Guid.NewGuid():N}";
        Factory.Destination.Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{\"received\":true}"));

        var response = await PostDispatch(new
        {
            templateId = SeedData.EfiPixConfirmadoId,
            destinationUrl = DestinationUrl(path),
            payloadOverrides = new Dictionary<string, string> { ["valor"] = "249.90", ["txid"] = "TXIDTESTE123" },
            customHeaders = new Dictionary<string, string> { ["X-Correlation-Id"] = "corr-1" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var dispatch = await ReadAsync<DispatchDto>(response);
        dispatch.Success.Should().BeTrue();
        dispatch.ResponseStatusCode.Should().Be(200);

        var received = Factory.Destination.LogEntries.Single(l => l.RequestMessage.Path == path).RequestMessage;
        using var payload = JsonDocument.Parse(received.Body!);
        var pix = payload.RootElement.GetProperty("pix")[0];
        pix.GetProperty("valor").GetString().Should().Be("249.90");
        pix.GetProperty("txid").GetString().Should().Be("TXIDTESTE123");
        received.Headers!["X-Correlation-Id"].Should().ContainSingle().Which.Should().Be("corr-1");
        received.Headers!["User-Agent"].Single().Should().Contain("Efi-Webhook/1.0");
        received.Body.Should().Be(dispatch.RenderedPayload);
    }

    [Fact]
    public async Task PostDispatch_TemplateMercadoPago_EnviaAssinaturaHmacValida()
    {
        var path = $"/mp/{Guid.NewGuid():N}";
        Factory.Destination.Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));

        var response = await PostDispatch(new { templateId = SeedData.MpPixConfirmadoId, destinationUrl = DestinationUrl(path) });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var received = Factory.Destination.LogEntries.Single(l => l.RequestMessage.Path == path).RequestMessage;

        // A MP não assina o corpo cru: assina o manifest "id:{data.id};request-id:{x-request-id};ts:{ts};".
        // Por isso recalculamos o manifest a partir do próprio ts/request-id recebidos, em vez de gerar um novo ts.
        var signatureHeader = received.Headers!["x-signature"].Single();
        var parts = signatureHeader.Split(',').Select(p => p.Split('=', 2)).ToDictionary(kv => kv[0], kv => kv[1]);
        parts.Should().ContainKey("ts");
        parts.Should().ContainKey("v1");

        // Nomes de header HTTP são case-insensitive; o servidor que recebe pode normalizar a grafia
        // (ex.: "X-Request-Id"), então procuramos sem diferenciar maiúsculas/minúsculas.
        var requestId = received.Headers!
            .Single(h => string.Equals(h.Key, "x-request-id", StringComparison.OrdinalIgnoreCase)).Value.Single();
        using var payload = JsonDocument.Parse(received.Body!);
        var dataId = payload.RootElement.GetProperty("data").GetProperty("id").GetString();
        var manifest = $"id:{dataId!.ToLowerInvariant()};request-id:{requestId};ts:{parts["ts"]};";
        var expectedHex = HmacSha256SignatureStrategy.HmacHex(manifest, PixHookApiFactory.MercadoPagoSecret);

        parts["v1"].Should().Be(expectedHex);
    }

    [Fact]
    public async Task PostDispatch_DestinoResponde500_RegistraDisparoComFalha()
    {
        var path = $"/erro/{Guid.NewGuid():N}";
        Factory.Destination.Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("falhou"));

        var response = await PostDispatch(new { templateId = SeedData.EfiPixExpiradoId, destinationUrl = DestinationUrl(path) });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dispatch = await ReadAsync<DispatchDto>(response);
        dispatch.Success.Should().BeFalse();
        dispatch.ResponseStatusCode.Should().Be(500);
        dispatch.ResponseBody.Should().Be("falhou");
    }

    [Fact]
    public async Task PostDispatch_DestinoNaoResponde_RegistraTimeout()
    {
        var path = $"/lento/{Guid.NewGuid():N}";
        Factory.Destination.Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(PixHookApiFactory.DispatchTimeoutSeconds + 3)));

        var response = await PostDispatch(new { templateId = SeedData.EfiPixConfirmadoId, destinationUrl = DestinationUrl(path) });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dispatch = await ReadAsync<DispatchDto>(response);
        dispatch.Success.Should().BeFalse();
        dispatch.ResponseStatusCode.Should().BeNull();
        dispatch.ErrorMessage.Should().Contain("Timeout");
    }

    [Fact]
    public async Task PostDispatch_DestinoIndisponivel_RegistraFalhaDeConexao()
    {
        var response = await PostDispatch(new { templateId = SeedData.EfiPixConfirmadoId, destinationUrl = "http://127.0.0.1:1/fechado" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dispatch = await ReadAsync<DispatchDto>(response);
        dispatch.Success.Should().BeFalse();
        dispatch.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PostDispatch_TemplateInexistente_Retorna404ProblemDetails()
    {
        var response = await PostDispatch(new { templateId = Guid.NewGuid(), destinationUrl = "http://localhost:5000/x" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task PostDispatch_OverrideQuebraJson_Retorna422ProblemDetails()
    {
        // Mercado Pago usa {{paymentId}} sem aspas (número): um valor não numérico quebra o JSON.
        var response = await PostDispatch(new
        {
            templateId = SeedData.MpPixConfirmadoId,
            destinationUrl = "http://localhost:5000/x",
            payloadOverrides = new Dictionary<string, string> { ["paymentId"] = "nao-numerico" }
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("code").GetString().Should().Be("payload.invalid_json");
    }

    [Fact]
    public async Task PostDispatch_UrlInvalida_Retorna400ComErrosDeValidacao()
    {
        var response = await PostDispatch(new { templateId = SeedData.EfiPixConfirmadoId, destinationUrl = "nao-e-url" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("errors").TryGetProperty("DestinationUrl", out _).Should().BeTrue();
    }

    [Fact]
    public async Task PostReplay_DisparoAntigo_ReenviaMesmoPayloadEReferenciaOriginal()
    {
        var path = $"/replay/{Guid.NewGuid():N}";
        Factory.Destination.Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));

        var original = await ReadAsync<DispatchDto>(await PostDispatch(
            new { templateId = SeedData.EfiPixValorDivergenteId, destinationUrl = DestinationUrl(path) }));

        var response = await Client.PostAsync($"/api/v1/dispatches/{original.Id}/replay", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var replay = await ReadAsync<DispatchDto>(response);
        replay.Id.Should().NotBe(original.Id);
        replay.ReplayOfDispatchId.Should().Be(original.Id);
        replay.RenderedPayload.Should().Be(original.RenderedPayload);

        var bodies = Factory.Destination.LogEntries.Where(l => l.RequestMessage.Path == path).Select(l => l.RequestMessage.Body).ToList();
        bodies.Should().HaveCount(2).And.OnlyContain(b => b == original.RenderedPayload);
    }

    [Fact]
    public async Task PostReplay_DisparoInexistente_Retorna404()
    {
        var response = await Client.PostAsync($"/api/v1/dispatches/{Guid.NewGuid()}/replay", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDispatches_VariosDisparos_PaginaEFiltraPorProvedor()
    {
        var path = $"/hist/{Guid.NewGuid():N}";
        Factory.Destination.Given(Request.Create().WithPath(path).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));
        for (var i = 0; i < 3; i++)
            await PostDispatch(new { templateId = SeedData.MpPixExpiradoId, destinationUrl = DestinationUrl(path) });

        var page1 = await ReadAsync<PagedResult<DispatchSummaryDto>>(
            await Client.GetAsync($"/api/v1/dispatches?page=1&pageSize=2&providerId={SeedData.MercadoPagoId}"));

        page1.Items.Should().HaveCount(2);
        page1.TotalCount.Should().BeGreaterThanOrEqualTo(3);
        page1.Items.Should().OnlyContain(i => i.ProviderName == "Mercado Pago");
        page1.Items.Should().BeInDescendingOrder(i => i.RequestedAt);

        var page2 = await ReadAsync<PagedResult<DispatchSummaryDto>>(
            await Client.GetAsync($"/api/v1/dispatches?page=2&pageSize=2&providerId={SeedData.MercadoPagoId}"));
        page2.Items.Select(i => i.Id).Should().NotIntersectWith(page1.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task GetDispatches_PageSizeInvalido_Retorna400()
    {
        var response = await Client.GetAsync("/api/v1/dispatches?pageSize=500");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDispatchById_Inexistente_Retorna404()
    {
        var response = await Client.GetAsync($"/api/v1/dispatches/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
