using FluentValidation;
using MediatR;
using PixHook.Application.Common;
using PixHook.Domain.Repositories;

namespace PixHook.Application.Features.Dispatches;

public sealed record GetDispatchHistoryQuery(int Page = 1, int PageSize = 20, Guid? ProviderId = null)
    : IRequest<Result<PagedResult<DispatchSummaryDto>>>;

public sealed class GetDispatchHistoryQueryValidator : AbstractValidator<GetDispatchHistoryQuery>
{
    public const int MaxPageSize = 100;

    public GetDispatchHistoryQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("page deve ser >= 1.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize).WithMessage($"pageSize deve estar entre 1 e {MaxPageSize}.");
    }
}

public sealed class GetDispatchHistoryQueryHandler
    : IRequestHandler<GetDispatchHistoryQuery, Result<PagedResult<DispatchSummaryDto>>>
{
    private readonly IDispatchRepository _dispatches;

    public GetDispatchHistoryQueryHandler(IDispatchRepository dispatches) => _dispatches = dispatches;

    public async Task<Result<PagedResult<DispatchSummaryDto>>> Handle(GetDispatchHistoryQuery request, CancellationToken ct)
    {
        var (items, total) = await _dispatches.GetPagedAsync(request.Page, request.PageSize, request.ProviderId, ct);
        return new PagedResult<DispatchSummaryDto>(
            items.Select(DispatchSummaryDto.From).ToList(), request.Page, request.PageSize, total);
    }
}
