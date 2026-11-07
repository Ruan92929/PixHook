using Microsoft.AspNetCore.Mvc;
using PixHook.Application.Common;

namespace PixHook.Api.Extensions;

/// <summary>Converte <see cref="Error"/> do Result Pattern em respostas ProblemDetails (RFC 7807).</summary>
public static class ResultExtensions
{
    private static readonly System.Text.Json.JsonSerializerOptions ProblemJsonOptions =
        new(System.Text.Json.JsonSerializerDefaults.Web);

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

        // JsonResult (e não ObjectResult) para que o [Produces("application/json")] dos controllers
        // não sobrescreva o Content-Type exigido pela RFC 7807.
        return new JsonResult(problem, ProblemJsonOptions)
        {
            StatusCode = status,
            ContentType = "application/problem+json"
        };
    }
}
