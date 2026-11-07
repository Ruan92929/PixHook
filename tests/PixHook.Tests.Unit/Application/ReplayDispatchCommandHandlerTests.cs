using Microsoft.Extensions.Logging.Abstractions;
using PixHook.Application.Abstractions;
using PixHook.Application.Common;
using PixHook.Application.Features.Dispatches;
using PixHook.Domain.Entities;
using PixHook.Domain.Repositories;

namespace PixHook.Tests.Unit.Application;

public class ReplayDispatchCommandHandlerTests
{
    private readonly IDispatchRepository _dispatches = Substitute.For<IDispatchRepository>();
    private readonly IWebhookDispatcher _dispatcher = Substitute.For<IWebhookDispatcher>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ReplayDispatchCommandHandler _sut;

    public ReplayDispatchCommandHandlerTests()
    {
        _clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        _sut = new ReplayDispatchCommandHandler(_dispatches, _dispatcher, _clock, NullLogger<ReplayDispatchCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_DisparoInexistente_RetornaNotFound()
    {
        _dispatches.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WebhookDispatch?)null);

        var result = await _sut.Handle(new ReplayDispatchCommand(Guid.NewGuid()), default);

        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_DisparoExistente_ReenviaMesmoPayloadEHeaders()
    {
        var original = WebhookDispatch.Create(Guid.NewGuid(), "http://localhost:5000/hook",
            "{\"txid\":\"T\"}", "{\"X-Test\":\"1\"}", DateTimeOffset.UtcNow.AddHours(-1));
        _dispatches.GetByIdAsync(original.Id, Arg.Any<CancellationToken>()).Returns(original);
        _dispatcher.SendAsync(default!, default!, default!, default).ReturnsForAnyArgs(DispatchResult.Ok(202, null, 5));

        var result = await _sut.Handle(new ReplayDispatchCommand(original.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.ReplayOfDispatchId.Should().Be(original.Id);
        result.Value.ResponseStatusCode.Should().Be(202);
        await _dispatcher.Received(1).SendAsync("http://localhost:5000/hook", "{\"txid\":\"T\"}",
            Arg.Is<IDictionary<string, string>>(h => h["X-Test"] == "1"), Arg.Any<CancellationToken>());
        await _dispatches.Received(1).AddAsync(Arg.Is<WebhookDispatch>(d => d.ReplayOfDispatchId == original.Id), Arg.Any<CancellationToken>());
    }
}
