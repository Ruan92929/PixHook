using MediatR;
using PixHook.Application.Common;
using PixHook.Domain.Repositories;

namespace PixHook.Application.Features.Scenarios;

public sealed record GetScenariosQuery : IRequest<Result<IReadOnlyList<ScenarioDto>>>;

public sealed class GetScenariosQueryHandler : IRequestHandler<GetScenariosQuery, Result<IReadOnlyList<ScenarioDto>>>
{
    private readonly IScenarioRepository _scenarios;

    public GetScenariosQueryHandler(IScenarioRepository scenarios) => _scenarios = scenarios;

    public async Task<Result<IReadOnlyList<ScenarioDto>>> Handle(GetScenariosQuery request, CancellationToken ct)
    {
        var scenarios = await _scenarios.GetActiveAsync(ct);
        return Result<IReadOnlyList<ScenarioDto>>.Success(scenarios.Select(ScenarioDto.From).ToList());
    }
}
