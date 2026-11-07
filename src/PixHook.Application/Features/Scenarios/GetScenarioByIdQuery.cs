using MediatR;
using PixHook.Application.Common;
using PixHook.Domain.Repositories;

namespace PixHook.Application.Features.Scenarios;

public sealed record GetScenarioByIdQuery(Guid Id) : IRequest<Result<ScenarioDto>>;

public sealed class GetScenarioByIdQueryHandler : IRequestHandler<GetScenarioByIdQuery, Result<ScenarioDto>>
{
    private readonly IScenarioRepository _scenarios;

    public GetScenarioByIdQueryHandler(IScenarioRepository scenarios) => _scenarios = scenarios;

    public async Task<Result<ScenarioDto>> Handle(GetScenarioByIdQuery request, CancellationToken ct)
    {
        var scenario = await _scenarios.GetByIdAsync(request.Id, ct);
        return scenario is null
            ? Error.NotFound("scenario.not_found", $"Cenário '{request.Id}' não encontrado.")
            : ScenarioDto.From(scenario);
    }
}
