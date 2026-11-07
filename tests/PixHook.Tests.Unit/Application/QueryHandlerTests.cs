using PixHook.Application.Common;
using PixHook.Application.Features.Dispatches;
using PixHook.Application.Features.Providers;
using PixHook.Domain.Entities;
using PixHook.Domain.Repositories;
using PixHook.Tests.Unit.Builders;

namespace PixHook.Tests.Unit.Application;

public class QueryHandlerTests
{
    private readonly IProviderRepository _providers = Substitute.For<IProviderRepository>();
    private readonly IDispatchRepository _dispatches = Substitute.For<IDispatchRepository>();

    [Fact]
    public async Task GetProviderEvents_ProviderInexistente_RetornaNotFound()
    {
        _providers.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WebhookProvider?)null);

        var result = await new GetProviderEventsQueryHandler(_providers).Handle(new GetProviderEventsQuery(Guid.NewGuid()), default);

        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetProviderEvents_ProviderExistente_RetornaTemplatesComPlaceholders()
    {
        var provider = TestData.Provider();
        _providers.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        _providers.GetTemplatesByProviderAsync(provider.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { TestData.Template(provider.Id) });

        var result = await new GetProviderEventsQueryHandler(_providers).Handle(new GetProviderEventsQuery(provider.Id), default);

        result.Value.Should().ContainSingle().Which.Placeholders.Should().Contain(new[] { "txid", "valor", "dataHora" });
    }

    [Fact]
    public async Task GetProviders_ComProvedoresAtivos_RetornaLista()
    {
        _providers.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(new[] { TestData.Provider(), TestData.Provider() });

        var result = await new GetProvidersQueryHandler(_providers).Handle(new GetProvidersQuery(), default);

        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDispatchHistory_ComItens_RetornaPaginaComTotais()
    {
        var items = Enumerable.Range(0, 5)
            .Select(_ => WebhookDispatch.Create(Guid.NewGuid(), "http://x", "{}", "{}", DateTimeOffset.UtcNow))
            .ToList();
        _dispatches.GetPagedAsync(2, 5, null, Arg.Any<CancellationToken>()).Returns((items, 12));

        var result = await new GetDispatchHistoryQueryHandler(_dispatches).Handle(new GetDispatchHistoryQuery(2, 5), default);

        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(12);
        result.Value.TotalPages.Should().Be(3);
        result.Value.HasNextPage.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetDispatchById_Inexistente_RetornaNotFound()
    {
        var result = await new GetDispatchByIdQueryHandler(_dispatches).Handle(new GetDispatchByIdQuery(Guid.NewGuid()), default);

        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }
}
