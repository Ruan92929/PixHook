using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PixHook.Api.Contracts;
using PixHook.Api.Extensions;
using PixHook.Api.Swagger.Examples;
using PixHook.Application.Common;
using PixHook.Application.Features.Dispatches;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace PixHook.Api.Controllers;

/// <summary>Disparo de webhooks, histórico e replay.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dispatches")]
[Produces("application/json")]
[Tags("Dispatches")]
public sealed class DispatchesController : ControllerBase
{
    private readonly ISender _sender;

    public DispatchesController(ISender sender) => _sender = sender;

    /// <summary>Dispara um webhook simulado para a URL de destino.</summary>
    /// <remarks>
    /// Renderiza o template escolhido (placeholders recebem valores padrão realistas, sobrescrevíveis via
    /// <c>payloadOverrides</c>), aplica assinatura do provedor quando houver e envia um POST para
    /// <c>destinationUrl</c>. O disparo é sempre registrado no histórico — mesmo quando o destino falha
    /// (nesse caso <c>success = false</c> e <c>errorMessage</c> preenchido).
    /// </remarks>
    /// <param name="request">Template, URL de destino, overrides de payload e headers extras.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <response code="201">Disparo executado e registrado.</response>
    /// <response code="400">Requisição inválida (ex.: URL de destino malformada).</response>
    /// <response code="404">Template inexistente.</response>
    /// <response code="422">Payload renderizado inválido (JSON quebrado ou placeholders sem valor).</response>
    [HttpPost]
    [SwaggerOperation(OperationId = "DispatchWebhook")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(DispatchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    [SwaggerRequestExample(typeof(DispatchWebhookRequest), typeof(DispatchWebhookRequestExample))]
    [SwaggerResponseExample(StatusCodes.Status201Created, typeof(DispatchExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(BadRequestProblemExample))]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundProblemExample))]
    [SwaggerResponseExample(StatusCodes.Status422UnprocessableEntity, typeof(UnprocessableProblemExample))]
    public async Task<IActionResult> Dispatch([FromBody] DispatchWebhookRequest request, CancellationToken ct)
    {
        var command = new DispatchWebhookCommand(
            request.TemplateId, request.DestinationUrl, request.PayloadOverrides, request.CustomHeaders);

        var result = await _sender.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id, version = "1.0" }, result.Value)
            : this.ToProblem(result.Error!);
    }

    /// <summary>Histórico paginado de disparos (mais recentes primeiro).</summary>
    /// <param name="page">Página (a partir de 1).</param>
    /// <param name="pageSize">Itens por página (1 a 100).</param>
    /// <param name="providerId">Filtra por provedor (opcional).</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <response code="200">Página do histórico.</response>
    /// <response code="400">Parâmetros de paginação inválidos.</response>
    [HttpGet]
    [SwaggerOperation(OperationId = "ListDispatches")]
    [ProducesResponseType(typeof(PagedResult<DispatchSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(DispatchHistoryExample))]
    [SwaggerResponseExample(StatusCodes.Status400BadRequest, typeof(BadRequestProblemExample))]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? providerId = null,
        CancellationToken ct = default)
    {
        var result = await _sender.Send(new GetDispatchHistoryQuery(page, pageSize, providerId), ct);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>Detalhe de um disparo (payload e headers enviados, resposta do destino).</summary>
    /// <param name="id">Id do disparo.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <response code="200">Disparo encontrado.</response>
    /// <response code="404">Disparo inexistente.</response>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(OperationId = "GetDispatch")]
    [ProducesResponseType(typeof(DispatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(DispatchExample))]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundProblemExample))]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetDispatchByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }

    /// <summary>Reenvia (replay) um disparo anterior com o mesmo payload, headers e URL.</summary>
    /// <remarks>Cria um novo registro com <c>replayOfDispatchId</c> apontando para o original.</remarks>
    /// <param name="id">Id do disparo original.</param>
    /// <param name="ct">Token de cancelamento.</param>
    /// <response code="201">Replay executado e registrado.</response>
    /// <response code="404">Disparo original inexistente.</response>
    [HttpPost("{id:guid}/replay")]
    [SwaggerOperation(OperationId = "ReplayDispatch")]
    [ProducesResponseType(typeof(DispatchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [SwaggerResponseExample(StatusCodes.Status201Created, typeof(ReplayDispatchExample))]
    [SwaggerResponseExample(StatusCodes.Status404NotFound, typeof(NotFoundProblemExample))]
    public async Task<IActionResult> Replay([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new ReplayDispatchCommand(id), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id, version = "1.0" }, result.Value)
            : this.ToProblem(result.Error!);
    }
}
