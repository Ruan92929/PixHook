using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PixHook.Application.Abstractions;
using PixHook.Application.Common;
using PixHook.Domain.Repositories;

namespace PixHook.Application.Features.Dispatches;

/// <summary>Reenvia um disparo anterior com exatamente o mesmo payload, headers e URL.</summary>
public sealed record ReplayDispatchCommand(Guid DispatchId) : IRequest<Result<DispatchDto>>;

public sealed class ReplayDispatchCommandValidator : AbstractValidator<ReplayDispatchCommand>
{
    public ReplayDispatchCommandValidator() =>
        RuleFor(x => x.DispatchId).NotEmpty().WithMessage("id do disparo é obrigatório.");
}

public sealed class ReplayDispatchCommandHandler : IRequestHandler<ReplayDispatchCommand, Result<DispatchDto>>
{
    private readonly IDispatchRepository _dispatches;
    private readonly IWebhookDispatcher _dispatcher;
    private readonly IClock _clock;
    private readonly ILogger<ReplayDispatchCommandHandler> _logger;

    public ReplayDispatchCommandHandler(
        IDispatchRepository dispatches, IWebhookDispatcher dispatcher, IClock clock,
        ILogger<ReplayDispatchCommandHandler> logger)
    {
        _dispatches = dispatches;
        _dispatcher = dispatcher;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<DispatchDto>> Handle(ReplayDispatchCommand request, CancellationToken ct)
    {
        var original = await _dispatches.GetByIdAsync(request.DispatchId, ct);
        if (original is null)
            return Error.NotFound("dispatch.not_found", $"Disparo '{request.DispatchId}' não encontrado.");

        var replay = original.CreateReplay(_clock.UtcNow);
        var headers = new Dictionary<string, string>(DispatchDto.DeserializeHeaders(original.RenderedHeaders));

        var response = await _dispatcher.SendAsync(replay.DestinationUrl, replay.RenderedPayload, headers, ct);
        replay.RegisterResponse(response.StatusCode, response.ResponseBody, response.ResponseTimeMs, response.Success, response.ErrorMessage);

        await _dispatches.AddAsync(replay, ct);
        await _dispatches.SaveChangesAsync(ct);

        _logger.LogInformation("Replay {ReplayId} do disparo {OriginalId}: sucesso={Success}",
            replay.Id, original.Id, response.Success);

        var saved = await _dispatches.GetByIdAsync(replay.Id, ct) ?? replay;
        return DispatchDto.From(saved);
    }
}
