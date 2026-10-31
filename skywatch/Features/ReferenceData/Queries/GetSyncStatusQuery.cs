using MediatR;
using SkyWatch.Features.ReferenceData.Dtos;
using SkyWatch.Repositories;

namespace SkyWatch.Features.ReferenceData.Queries;

public record GetSyncStatusQuery : IRequest<IReadOnlyList<SyncStateReturnDto>>;

public class GetSyncStatusHandler(IUnitOfWork uow) : IRequestHandler<GetSyncStatusQuery, IReadOnlyList<SyncStateReturnDto>>
{
    public async Task<IReadOnlyList<SyncStateReturnDto>> Handle(GetSyncStatusQuery request, CancellationToken ct) =>
        (await uow.SyncStates.ListAsync(ct))
            .Select(s => new SyncStateReturnDto(s.Source, s.Status, s.LastSyncedAt, s.LastAttemptAt, s.Rows, s.Added, s.Updated, s.Deactivated, s.Error))
            .ToList();
}
