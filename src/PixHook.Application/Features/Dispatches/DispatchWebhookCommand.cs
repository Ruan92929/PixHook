using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PixHook.Application.Abstractions;
using PixHook.Application.Common;
using PixHook.Application.Services;
using PixHook.Domain.Entities;
using PixHook.Domain.Repositories;

namespace PixHook.Application.Features.Dispatches;

public sealed record DispatchWebhookCommand(
    Guid TemplateId,
    string DestinationUrl,
    IDictionary<string, string>? PayloadOverrides = null,
    IDictionary<string, string>? CustomHeaders = null) : IRequest<Result<DispatchDto>>;

public sealed class DispatchWebhookCommandValidator : AbstractValidator<DispatchWebhookCommand>
{
    public DispatchWebhookCommandValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("templateId é obrigatório.");

        RuleFor(x => x.DestinationUrl)
            .NotEmpty().WithMessage("destinationUrl é obrigatória.")
            .Must(BeAbsoluteHttpUrl).WithMessage("destinationUrl deve ser uma URL absoluta http(s).");

        RuleForEach(x => x.CustomHeaders)
            .Must(h => !string.IsNullOrWhiteSpace(h.Key))
            .WithMessage("Nome de header não pode ser vazio.")
            .When(x => x.CustomHeaders is not null);
    }

    internal static bool BeAbsoluteHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public sealed class DispatchWebhookCommandHandler : IRequestHandler<DispatchWebhookCommand, Result<DispatchDto>>
{
    private readonly IProviderRepository _providers;
    private readonly IDispatchRepository _dispatches;
    private readonly IWebhookRequestBuilder _builder;
    private readonly IWebhookDispatcher _dispatcher;
    private readonly IClock _clock;
    private readonly ILogger<DispatchWebhookCommandHandler> _logger;

    public DispatchWebhookCommandHandler(
        IProviderRepository providers,
        IDispatchRepository dispatches,
        IWebhookRequestBuilder builder,
        IWebhookDispatcher dispatcher,
        IClock clock,
        ILogger<DispatchWebhookCommandHandler> logger)
    {
        _providers = providers;
        _dispatches = dispatches;
        _builder = builder;
        _dispatcher = dispatcher;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<DispatchDto>> Handle(DispatchWebhookCommand request, CancellationToken ct)
    {
        var template = await _providers.GetTemplateByIdAsync(request.TemplateId, ct);
        if (template is null)
            return Error.NotFound("template.not_found", $"Template '{request.TemplateId}' não encontrado.");

        var built = _builder.Build(template, request.PayloadOverrides, request.CustomHeaders);
        if (built.IsFailure) return built.Error!;

        var dispatch = WebhookDispatch.Create(
            template.Id, request.DestinationUrl, built.Value.Payload,
            JsonSerializer.Serialize(built.Value.Headers), _clock.UtcNow);

        var response = await _dispatcher.SendAsync(request.DestinationUrl, built.Value.Payload, built.Value.Headers, ct);
        dispatch.RegisterResponse(response.StatusCode, response.ResponseBody, response.ResponseTimeMs, response.Success, response.ErrorMessage);

        await _dispatches.AddAsync(dispatch, ct);
        await _dispatches.SaveChangesAsync(ct);

        _logger.LogInformation("Webhook {DispatchId} ({Template}) disparado para {Url}: sucesso={Success} status={Status}",
            dispatch.Id, template.Name, request.DestinationUrl, response.Success, response.StatusCode);

        var saved = await _dispatches.GetByIdAsync(dispatch.Id, ct) ?? dispatch;
        return DispatchDto.From(saved);
    }
}
