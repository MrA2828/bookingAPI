using MediatR;
using SkyWatch.Features.ReferenceData.Dtos;
using SkyWatch.Repositories;

namespace SkyWatch.Features.ReferenceData.Queries;

public record GetSyncStatusQuery : IRequest<IReadOnlyList<SyncStateDto>>;

public class GetSyncStatusHandler(IUnitOfWork uow) : IRequestHandler<GetSyncStatusQuery, IReadOnlyList<SyncStateDto>>
{
    public async Task<IReadOnlyList<SyncStateDto>> Handle(GetSyncStatusQuery request, CancellationToken ct) =>
        (await uow.SyncStates.ListAsync(ct))
            .Select(s => new SyncStateDto(s.Source, s.Status, s.LastSyncedAt, s.LastAttemptAt, s.Rows, s.Added, s.Updated, s.Deactivated, s.Error))
            .ToList();
}
