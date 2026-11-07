using Microsoft.AspNetCore.Mvc;
using PixHook.Api.Contracts;
using PixHook.Application.Common;
using PixHook.Application.Features.Dispatches;
using PixHook.Domain.Enums;
using PixHook.Infrastructure.Persistence.Seed;
using Swashbuckle.AspNetCore.Filters;

namespace PixHook.Api.Swagger.Examples;

internal static class ExampleData
{
    public static readonly Guid DispatchId = Guid.Parse("c0ffee00-1234-4abc-8def-000000000001");
    public static readonly DateTimeOffset RequestedAt = new(2026, 11, 7, 14, 30, 0, TimeSpan.Zero);

    public const string Payload =
        "{\"pix\":[{\"endToEndId\":\"E1803615020261107143012345678901\",\"txid\":\"7978c0c97ea847e78e8849634473c1f1\"," +
        "\"chave\":\"pagamentos@lojaexemplo.com.br\",\"valor\":\"249.90\",\"horario\":\"2026-11-07T14:30:00.000+00:00\"}]}";

    public static DispatchDto Dispatch(Guid? replayOf = null) => new(
        replayOf is null ? DispatchId : Guid.Parse("c0ffee00-1234-4abc-8def-000000000002"),
        SeedData.EfiPixConfirmadoId, "PIX confirmado", EventType.PixConfirmado, SeedData.EfiId, "Efi (Gerencianet)",
        "http://localhost:5000/webhooks/pix", Payload,
        new Dictionary<string, string> { ["User-Agent"] = "Efi-Webhook/1.0" },
        RequestedAt, 200, "{\"received\":true}", 42, true, null, replayOf);
}

public sealed class DispatchWebhookRequestExample : IExamplesProvider<DispatchWebhookRequest>
{
    public DispatchWebhookRequest GetExamples() => new()
    {
        TemplateId = SeedData.EfiPixConfirmadoId,
        DestinationUrl = "http://localhost:5000/webhooks/pix",
        PayloadOverrides = new Dictionary<string, string>
        {
            ["valor"] = "249.90",
            ["txid"] = "7978c0c97ea847e78e8849634473c1f1"
        },
        CustomHeaders = new Dictionary<string, string> { ["X-Correlation-Id"] = "teste-local-001" }
    };
}

public sealed class DispatchExample : IExamplesProvider<DispatchDto>
{
    public DispatchDto GetExamples() => ExampleData.Dispatch();
}

public sealed class ReplayDispatchExample : IMultipleExamplesProvider<DispatchDto>
{
    public IEnumerable<SwaggerExample<DispatchDto>> GetExamples()
    {
        yield return SwaggerExample.Create("Replay", "Reenvio de um disparo anterior",
            ExampleData.Dispatch(replayOf: ExampleData.DispatchId));
    }
}

public sealed class DispatchHistoryExample : IExamplesProvider<PagedResult<DispatchSummaryDto>>
{
    public PagedResult<DispatchSummaryDto> GetExamples() => new(
        new[]
        {
            new DispatchSummaryDto(ExampleData.DispatchId, SeedData.EfiPixConfirmadoId, "PIX confirmado",
                EventType.PixConfirmado, "Efi (Gerencianet)", "http://localhost:5000/webhooks/pix",
                ExampleData.RequestedAt, 200, 42, true, null),
            new DispatchSummaryDto(Guid.Parse("c0ffee00-1234-4abc-8def-000000000003"), SeedData.MpPixExpiradoId,
                "PIX expirado", EventType.PixExpirado, "Mercado Pago", "http://localhost:5000/webhooks/mp",
                ExampleData.RequestedAt.AddMinutes(-5), 500, 120, false, null)
        }, Page: 1, PageSize: 20, TotalCount: 2);
}

public sealed class BadRequestProblemExample : IExamplesProvider<ValidationProblemDetails>
{
    public ValidationProblemDetails GetExamples() => new(new Dictionary<string, string[]>
    {
        ["DestinationUrl"] = new[] { "destinationUrl deve ser uma URL absoluta http(s)." }
    })
    {
        Status = 400,
        Title = "Requisição inválida",
        Detail = "Um ou mais campos são inválidos.",
        Type = "https://httpstatuses.io/400",
        Extensions = { ["code"] = "validation_failed" }
    };
}

public sealed class NotFoundProblemExample : IExamplesProvider<ProblemDetails>
{
    public ProblemDetails GetExamples() => new()
    {
        Status = 404,
        Title = "Recurso não encontrado",
        Detail = "Template '00000000-0000-0000-0000-000000000099' não encontrado.",
        Type = "https://httpstatuses.io/404",
        Extensions = { ["code"] = "template.not_found" }
    };
}

public sealed class UnprocessableProblemExample : IExamplesProvider<ProblemDetails>
{
    public ProblemDetails GetExamples() => new()
    {
        Status = 422,
        Title = "Entidade não processável",
        Detail = "O payload renderizado não é um JSON válido: '\"' is invalid after a value.",
        Type = "https://httpstatuses.io/422",
        Extensions = { ["code"] = "payload.invalid_json" }
    };
}
