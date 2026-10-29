using MediatR;
using SkyWatch.Features.Airports.Dtos;
using SkyWatch.Repositories;

namespace SkyWatch.Features.Airports.Queries;

public record SearchAirlinesQuery(string? Search, int PageNumber, int PageSize) : IRequest<IReadOnlyList<AirlineDto>>;

public class SearchAirlinesHandler(IUnitOfWork uow) : IRequestHandler<SearchAirlinesQuery, IReadOnlyList<AirlineDto>>
{
    public async Task<IReadOnlyList<AirlineDto>> Handle(SearchAirlinesQuery r, CancellationToken ct) =>
        (await uow.Airlines.SearchAsync(r.Search, Math.Max(r.PageNumber, 1), Math.Clamp(r.PageSize, 1, 100), ct))
            .Select(a => new AirlineDto(a.Id, a.Name, a.IataCode, a.IcaoCode, a.Callsign, a.Country)).ToList();
}
