using MediatR;
using SkyWatch.Features.Airports.Dtos;
using SkyWatch.Repositories;

namespace SkyWatch.Features.Airports.Queries;

public record SearchAirportsQuery(string? Search, string? Country, string? Type, int Limit, int Offset) : IRequest<IReadOnlyList<AirportDto>>;

public class SearchAirportsHandler(IUnitOfWork uow) : IRequestHandler<SearchAirportsQuery, IReadOnlyList<AirportDto>>
{
    public async Task<IReadOnlyList<AirportDto>> Handle(SearchAirportsQuery r, CancellationToken ct) =>
        (await uow.Airports.SearchAsync(r.Search, r.Country, r.Type, Math.Clamp(r.Limit, 1, 100), Math.Max(r.Offset, 0), ct))
            .Select(AirportDto.From).ToList();
}
