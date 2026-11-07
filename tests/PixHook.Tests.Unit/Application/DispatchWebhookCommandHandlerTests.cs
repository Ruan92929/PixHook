using Microsoft.Extensions.Logging.Abstractions;
using PixHook.Application.Abstractions;
using PixHook.Application.Common;
using PixHook.Application.Features.Dispatches;
using PixHook.Application.Services;
using PixHook.Domain.Entities;
using PixHook.Domain.Repositories;
using PixHook.Domain.ValueObjects;
using PixHook.Tests.Unit.Builders;

namespace PixHook.Tests.Unit.Application;

public class DispatchWebhookCommandHandlerTests
{
    private readonly IProviderRepository _providers = Substitute.For<IProviderRepository>();
    private readonly IDispatchRepository _dispatches = Substitute.For<IDispatchRepository>();
    private readonly IWebhookDispatcher _dispatcher = Substitute.For<IWebhookDispatcher>();
    private readonly IPlaceholderFactory _placeholders = Substitute.For<IPlaceholderFactory>();
    private readonly ISignatureStrategyResolver _resolver = Substitute.For<ISignatureStrategyResolver>();
    private readonly IWebhookSecretProvider _secrets = Substitute.For<IWebhookSecretProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly DispatchWebhookCommandHandler _sut;

    public DispatchWebhookCommandHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 11, 7, 12, 0, 0, TimeSpan.Zero));
        _placeholders.CreateDefaults().Returns(PlaceholderValues.Empty
            .With("txid", "TX1").With("valor", "10.00").With("dataHora", "2026-11-07T12:00:00Z"));
        _secrets.GetSecret(Arg.Any<string>()).Returns("secret");

        var builder = new WebhookRequestBuilder(_placeholders, _resolver, _secrets);
        _sut = new DispatchWebhookCommandHandler(_providers, _dispatches, builder, _dispatcher, _clock,
            NullLogger<DispatchWebhookCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_TemplateInexistente_RetornaNotFound()
    {
        _providers.GetTemplateByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WebhookEventTemplate?)null);

        var result = await _sut.Handle(new DispatchWebhookCommand(Guid.NewGuid(), TestData.Url()), default);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await _dispatcher.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default!, default);
    }

    [Fact]
    public async Task Handle_DestinoRespondeSucesso_RegistraDisparoComSucesso()
    {
        var template = TestData.Template();
        var url = TestData.Url();
        _providers.GetTemplateByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);
        _dispatcher.SendAsync(url, Arg.Any<string>(), Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(DispatchResult.Ok(200, "ok", 15));

        var result = await _sut.Handle(new DispatchWebhookCommand(template.Id, url,
            new Dictionary<string, string> { ["valor"] = "99.90" }), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.ResponseStatusCode.Should().Be(200);
        result.Value.RenderedPayload.Should().Contain("\"valor\":\"99.90\"").And.Contain("TX1");
        await _dispatches.Received(1).AddAsync(Arg.Is<WebhookDispatch>(d => d.Success && d.TemplateId == template.Id), Arg.Any<CancellationToken>());
        await _dispatches.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DestinoRespondeErro_RegistraDisparoComFalha()
    {
        var template = TestData.Template();
        _providers.GetTemplateByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);
        _dispatcher.SendAsync(default!, default!, default!, default).ReturnsForAnyArgs(DispatchResult.HttpError(500, "boom", 20));

        var result = await _sut.Handle(new DispatchWebhookCommand(template.Id, TestData.Url()), default);

        result.IsSuccess.Should().BeTrue("o disparo foi executado, mesmo que o destino tenha falhado");
        result.Value.Success.Should().BeFalse();
        result.Value.ErrorMessage.Should().Contain("500");
    }

    [Fact]
    public async Task Handle_PayloadRenderizadoInvalido_RetornaUnprocessable()
    {
        var template = TestData.Template(payload: """{"valor": {{valor}} """); // JSON quebrado
        _providers.GetTemplateByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        var result = await _sut.Handle(new DispatchWebhookCommand(template.Id, TestData.Url()), default);

        result.Error!.Type.Should().Be(ErrorType.Unprocessable);
        result.Error.Code.Should().Be("payload.invalid_json");
    }

    [Fact]
    public async Task Handle_PlaceholderSemValor_RetornaUnprocessable()
    {
        var template = TestData.Template(payload: """{"x":"{{naoExiste}}"}""");
        _providers.GetTemplateByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);

        var result = await _sut.Handle(new DispatchWebhookCommand(template.Id, TestData.Url()), default);

        result.Error!.Code.Should().Be("payload.missing_placeholders");
    }

    [Fact]
    public async Task Handle_TemplateComAssinatura_EnviaHeaderAssinado()
    {
        var strategy = Substitute.For<ISignatureStrategy>();
        strategy.Key.Returns("fake");
        strategy.HeaderName.Returns("X-Fake-Signature");
        strategy.Sign(Arg.Any<string>(), "secret", Arg.Any<IReadOnlyDictionary<string, string>>()).Returns("assinado");
        _resolver.Resolve("fake").Returns(strategy);

        var template = TestData.Template(signatureKey: "fake");
        _providers.GetTemplateByIdAsync(template.Id, Arg.Any<CancellationToken>()).Returns(template);
        _dispatcher.SendAsync(default!, default!, default!, default).ReturnsForAnyArgs(DispatchResult.Ok(200, null, 1));

        await _sut.Handle(new DispatchWebhookCommand(template.Id, TestData.Url(),
            CustomHeaders: new Dictionary<string, string> { ["X-Extra"] = "1" }), default);

        await _dispatcher.Received(1).SendAsync(Arg.Any<string>(), Arg.Any<string>(),
            Arg.Is<IDictionary<string, string>>(h =>
                h["X-Fake-Signature"] == "assinado" && h["X-Extra"] == "1" && h["User-Agent"] == "Teste/1.0"),
            Arg.Any<CancellationToken>());
    }
}
