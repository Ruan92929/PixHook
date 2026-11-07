using MediatR;
using PixHook.Application.Common;
using PixHook.Domain.Repositories;

namespace PixHook.Application.Features.Dispatches;

public sealed record GetDispatchByIdQuery(Guid Id) : IRequest<Result<DispatchDto>>;

public sealed class GetDispatchByIdQueryHandler : IRequestHandler<GetDispatchByIdQuery, Result<DispatchDto>>
{
    private readonly IDispatchRepository _dispatches;

    public GetDispatchByIdQueryHandler(IDispatchRepository dispatches) => _dispatches = dispatches;

    public async Task<Result<DispatchDto>> Handle(GetDispatchByIdQuery request, CancellationToken ct)
    {
        var dispatch = await _dispatches.GetByIdAsync(request.Id, ct);
        return dispatch is null
            ? Error.NotFound("dispatch.not_found", $"Disparo '{request.Id}' não encontrado.")
            : DispatchDto.From(dispatch);
    }
}
