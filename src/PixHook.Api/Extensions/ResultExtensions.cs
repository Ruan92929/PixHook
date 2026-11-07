using Microsoft.AspNetCore.Mvc;
using PixHook.Application.Common;

namespace PixHook.Api.Extensions;

/// <summary>Converte <see cref="Error"/> do Result Pattern em respostas ProblemDetails (RFC 7807).</summary>
public static class ResultExtensions
{
    public static IActionResult ToProblem(this ControllerBase controller, Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unprocessable => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };

        var title = error.Type switch
        {
            ErrorType.Validation => "Requisição inválida",
            ErrorType.NotFound => "Recurso não encontrado",
            ErrorType.Unprocessable => "Entidade não processável",
            _ => "Erro interno"
        };

        ProblemDetails problem = error.Details is { Count: > 0 }
            ? new ValidationProblemDetails(error.Details.ToDictionary(k => k.Key, v => v.Value))
            : new ProblemDetails();

        problem.Status = status;
        problem.Title = title;
        problem.Detail = error.Message;
        problem.Type = $"https://httpstatuses.io/{status}";
        problem.Instance = controller.HttpContext.Request.Path;
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = controller.HttpContext.TraceIdentifier;

        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
