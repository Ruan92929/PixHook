using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PixHook.Api.Contracts;
using PixHook.Api.Extensions;
using PixHook.Application.Features.Scenarios;

namespace PixHook.Api.Controllers;

/// <summary>
/// Cenários: sequências de eventos com atraso entre eles, para testar como o seu sistema reage a
/// situações como webhook duplicado, correção de valor ou entrega fora de ordem — não só um
/// disparo isolado.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/scenarios")]
[Produces("application/json")]
[Tags("Scenarios")]
public sealed class ScenariosController : ControllerBase
{
    private readonly ISender _sender;

    public ScenariosController(ISender sender) => _sender = sender;

    /// <summary>Lista os cenários disponíveis.</summary>
    /// <param name="ct">Token de cancelamento.</param>
    /// <response code="200">Cenários disponíveis, com seus passos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ScenarioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScenarios(CancellationToken ct)
    {
        var result = await _sender.Send(new GetScenariosQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>Detalhe de um cenário e seus passos.</summary>
    /// <param name="id">Id do cenário.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <response code="200">Cenário encontrado.</response>
    /// <response code="404">Cenário inexistente.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ScenarioDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetScenarioByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>Executa um cenário: dispara cada passo na ordem, respeitando o atraso entre eles.</summary>
    /// <remarks>
    /// A chamada fica bloqueada até o último passo terminar (os cenários do seed levam poucos
    /// segundos no total). Se um passo falhar por um erro de configuração (ex.: template
    /// inexistente), a execução é interrompida e o erro desse passo é devolvido.
    /// </remarks>
    /// <param name="id">Id do cenário.</param>
    /// <param name="request">URL de destino e headers extras.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <response code="200">Cenário executado — resultado de cada passo.</response>
    /// <response code="400">Requisição inválida (ex.: URL de destino malformada).</response>
    /// <response code="404">Cenário ou algum template referenciado por ele não foi encontrado.</response>
    /// <response code="422">Payload de algum passo não pôde ser renderizado.</response>
    [HttpPost("{id:guid}/run")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ScenarioRunResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> Run([FromRoute] Guid id, [FromBody] RunScenarioRequest request, CancellationToken ct)
    {
        var command = new RunScenarioCommand(id, request.DestinationUrl, request.CustomHeaders);
        var result = await _sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}
