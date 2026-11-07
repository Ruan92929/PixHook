using MediatR;
using PixHook.Application.Common;
using PixHook.Domain.Repositories;

namespace PixHook.Application.Features.Providers;

public sealed record GetProvidersQuery : IRequest<Result<IReadOnlyList<ProviderDto>>>;

public sealed class GetProvidersQueryHandler : IRequestHandler<GetProvidersQuery, Result<IReadOnlyList<ProviderDto>>>
{
    private readonly IProviderRepository _providers;

    public GetProvidersQueryHandler(IProviderRepository providers) => _providers = providers;

    public async Task<Result<IReadOnlyList<ProviderDto>>> Handle(GetProvidersQuery request, CancellationToken ct)
    {
        var providers = await _providers.GetActiveAsync(ct);
        return Result<IReadOnlyList<ProviderDto>>.Success(providers.Select(ProviderDto.From).ToList());
    }
}
