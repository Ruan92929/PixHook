using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PixHook.Api.Middleware;

/// <summary>Captura exceções não tratadas e devolve ProblemDetails 500.</summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetails;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails)
    {
        _logger = logger;
        _problemDetails = problemDetails;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Erro não tratado em {Path}", httpContext.Request.Path);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno",
                Detail = "Ocorreu um erro inesperado. Consulte os logs com o traceId informado.",
                Type = "https://httpstatuses.io/500"
            }
        });
    }
}
