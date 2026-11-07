using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PixHook.Api.Extensions;
using PixHook.Api.Swagger.Examples;
using PixHook.Application.Features.Providers;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace PixHook.Api.Controllers;

/// <summary>Provedores de pagamento simulados e seus templates de evento.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/providers")]
[Produces("application/json")]
[Tags("Providers")]
public sealed class ProvidersController : ControllerBase
{
    private readonly ISender _sender;

    public ProvidersController(ISender sender) => _sender = sender;

    /// <summary>Lista os provedores de pagamento ativos.</summary>
    /// <remarks>Use o <c>id</c> retornado para listar os eventos disparáveis do provedor.</remarks>
    /// <param name="ct">Token de cancelamento.</param>
    /// <response code="200">Lista de provedores ativos.</response>
    [HttpGet]
    [SwaggerOperation(OperationId = "ListProviders")]
    [ProducesResponseType(typeof(IReadOnlyList<ProviderDto>), StatusCodes.Status200OK)]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(ProviderListExample))]
    public async Task<IActionResult> GetProviders(CancellationToken ct)
    {
        var result = await _sender.Send(new GetProvidersQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>Lista os templates de evento (PIX confirmado, expirado, valor divergente...) de um provedor.</summary>
    /// <remarks>Cada template traz o JSON com placeholders (<c>{{valor}}</c>, <c>{{txid}}</c>...) que podem ser sobrescritos no disparo.</remarks>
    /// <param name="id">Id do provedor.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <response code="200">Templates do provedor.</response>
    /// <response code="404">Provedor não encontrado.</response>
    [HttpGet("{id:guid}/events")]
    [SwaggerOperation(OperationId = "ListProviderEvents")]
    [ProducesResponseType(typeof(IReadOnlyList<EventTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(EventTemplateListExample))]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundProblemExample))]
    public async Task<IActionResult> GetProviderEvents([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetProviderEventsQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}
