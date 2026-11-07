using MediatR;
using PixHook.Application.Common;
using PixHook.Domain.Repositories;

namespace PixHook.Application.Features.Providers;

public sealed record GetProviderEventsQuery(Guid ProviderId) : IRequest<Result<IReadOnlyList<EventTemplateDto>>>;

public sealed class GetProviderEventsQueryHandler
    : IRequestHandler<GetProviderEventsQuery, Result<IReadOnlyList<EventTemplateDto>>>
{
    private readonly IProviderRepository _providers;

    public GetProviderEventsQueryHandler(IProviderRepository providers) => _providers = providers;

    public async Task<Result<IReadOnlyList<EventTemplateDto>>> Handle(GetProviderEventsQuery request, CancellationToken ct)
    {
        var provider = await _providers.GetByIdAsync(request.ProviderId, ct);
        if (provider is null)
            return Error.NotFound("provider.not_found", $"Provedor '{request.ProviderId}' não encontrado.");

        var templates = await _providers.GetTemplatesByProviderAsync(request.ProviderId, ct);
        return Result<IReadOnlyList<EventTemplateDto>>.Success(templates.Select(EventTemplateDto.From).ToList());
    }
}
