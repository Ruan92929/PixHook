using FluentValidation;
using PixHook.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;
using PixHook.Application.Common;
using PixHook.Application.Features.Dispatches;
using static PixHook.Application.Features.Dispatches.DispatchWebhookCommandValidator;
using PixHook.Domain.Repositories;

namespace PixHook.Application.Features.Scenarios;

/// <summary>Resultado de um passo já executado dentro de um cenário.</summary>
public sealed record ScenarioStepRunResultDto(int Order, string Note, int DelayAppliedMs, DispatchDto Dispatch);

/// <summary>Resultado de uma execução completa do cenário, passo a passo.</summary>
public sealed record ScenarioRunResultDto(
    Guid ScenarioId,
    string ScenarioName,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    IReadOnlyList<ScenarioStepRunResultDto> Steps);

/// <summary>
/// Executa um cenário: dispara cada passo na ordem, esperando o atraso configurado entre eles.
/// A chamada fica bloqueada até o último passo terminar — para os cenários do seed (poucos
/// segundos de atraso total) isso é aceitável; um cenário com atrasos longos devia virar um job
/// em segundo plano (ver seção de evolução no README).
/// </summary>
public sealed record RunScenarioCommand(
    Guid ScenarioId,
    string DestinationUrl,
    IDictionary<string, string>? CustomHeaders = null) : IRequest<Result<ScenarioRunResultDto>>;

public sealed class RunScenarioCommandValidator : AbstractValidator<RunScenarioCommand>
{
    public RunScenarioCommandValidator()
    {
        RuleFor(x => x.ScenarioId).NotEmpty().WithMessage("scenarioId é obrigatório.");
        RuleFor(x => x.DestinationUrl)
            .NotEmpty().WithMessage("destinationUrl é obrigatória.")
            .Must(BeAbsoluteHttpUrl)
            .WithMessage("destinationUrl deve ser uma URL absoluta http(s).");
    }
}

public sealed class RunScenarioCommandHandler : IRequestHandler<RunScenarioCommand, Result<ScenarioRunResultDto>>
{
    private readonly IScenarioRepository _scenarios;
    private readonly ISender _sender;
    private readonly IClock _clock;
    private readonly ILogger<RunScenarioCommandHandler> _logger;

    public RunScenarioCommandHandler(
        IScenarioRepository scenarios, ISender sender, IClock clock, ILogger<RunScenarioCommandHandler> logger)
    {
        _scenarios = scenarios;
        _sender = sender;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<ScenarioRunResultDto>> Handle(RunScenarioCommand request, CancellationToken ct)
    {
        var scenario = await _scenarios.GetByIdAsync(request.ScenarioId, ct);
        if (scenario is null)
            return Error.NotFound("scenario.not_found", $"Cenário '{request.ScenarioId}' não encontrado.");

        var startedAt = _clock.UtcNow;
        var results = new List<ScenarioStepRunResultDto>();

        _logger.LogInformation("Iniciando cenário {Scenario} ({Steps} passo(s)) para {Url}",
            scenario.Name, scenario.Steps.Count, request.DestinationUrl);

        foreach (var step in scenario.Steps.OrderBy(s => s.Order))
        {
            if (step.DelayBeforeMs > 0)
                await Task.Delay(step.DelayBeforeMs, ct);

            var dispatchResult = await _sender.Send(
                new DispatchWebhookCommand(step.TemplateId, request.DestinationUrl, null, request.CustomHeaders), ct);

            if (dispatchResult.IsFailure)
            {
                _logger.LogWarning("Cenário {Scenario} interrompido no passo {Order}: {Error}",
                    scenario.Name, step.Order, dispatchResult.Error!.Message);
                return dispatchResult.Error!;
            }

            results.Add(new ScenarioStepRunResultDto(step.Order, step.Note, step.DelayBeforeMs, dispatchResult.Value));
        }

        return new ScenarioRunResultDto(scenario.Id, scenario.Name, startedAt, _clock.UtcNow, results);
    }
}
