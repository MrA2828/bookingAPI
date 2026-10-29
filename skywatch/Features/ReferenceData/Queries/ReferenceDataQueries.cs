using MediatR;
using SkyWatch.Repositories;

namespace SkyWatch.Features.ReferenceData.Queries;

public record SyncStateDto(string Source, string Status, DateTime? LastSyncedAt, DateTime? LastAttemptAt,
    int Rows, int Added, int Updated, int Deactivated, string? Error);

public record CountryDto(string Code, string Name, string Continent);

public record GetSyncStatusQuery : IRequest<IReadOnlyList<SyncStateDto>>;

public record GetCountriesQuery : IRequest<IReadOnlyList<CountryDto>>;

public class GetSyncStatusHandler(IUnitOfWork uow) : IRequestHandler<GetSyncStatusQuery, IReadOnlyList<SyncStateDto>>
{
    public async Task<IReadOnlyList<SyncStateDto>> Handle(GetSyncStatusQuery request, CancellationToken ct) =>
        (await uow.SyncStates.ListAsync(ct))
            .Select(s => new SyncStateDto(s.Source, s.Status, s.LastSyncedAt, s.LastAttemptAt, s.Rows, s.Added, s.Updated, s.Deactivated, s.Error))
            .ToList();
}

public class GetCountriesHandler(IUnitOfWork uow) : IRequestHandler<GetCountriesQuery, IReadOnlyList<CountryDto>>
{
    public async Task<IReadOnlyList<CountryDto>> Handle(GetCountriesQuery request, CancellationToken ct) =>
        (await uow.Countries.ListAsync(ct)).Select(c => new CountryDto(c.Code, c.Name, c.Continent)).ToList();
}
