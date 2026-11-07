using System.Net;
using System.Text.Json;
using PixHook.Application.Features.Providers;
using PixHook.Infrastructure.Persistence.Seed;

namespace PixHook.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ProvidersEndpointsTests : ApiTestBase
{
    public ProvidersEndpointsTests(PixHookApiFactory factory) : base(factory) { }

    [Fact]
    public async Task GetProviders_SeedAplicado_RetornaMercadoPagoEEfi()
    {
        var response = await Client.GetAsync("/api/v1/providers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var providers = await ReadAsync<List<ProviderDto>>(response);
        providers.Select(p => p.Slug).Should().Contain(new[] { "mercado-pago", "efi" });
    }

    [Fact]
    public async Task GetProviderEvents_ProviderExistente_RetornaTresEventosPix()
    {
        var response = await Client.GetAsync($"/api/v1/providers/{SeedData.EfiId}/events");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await ReadAsync<List<EventTemplateDto>>(response);
        events.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetProviderEvents_ProviderInexistente_Retorna404ProblemDetails()
    {
        var response = await Client.GetAsync($"/api/v1/providers/{Guid.NewGuid()}/events");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(404);
        doc.RootElement.GetProperty("code").GetString().Should().Be("provider.not_found");
    }

    [Fact]
    public async Task Swagger_DocumentoV1_DisponivelComTagsEExemplos()
    {
        var response = await Client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("/api/v1/dispatches").And.Contain("\"Providers\"").And.Contain("\"Dispatches\"");
        body.Should().Contain("\"example\"");
    }
}
