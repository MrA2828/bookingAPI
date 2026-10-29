using MediatR;
using SkyWatch.Features.Airports.Dtos;
using SkyWatch.Repositories;

namespace SkyWatch.Features.Airports.Queries;

public record SearchAirportsQuery(string? Search, string? Country, string? Type, int PageNumber, int PageSize) : IRequest<IReadOnlyList<AirportDto>>;

public class SearchAirportsHandler(IUnitOfWork uow) : IRequestHandler<SearchAirportsQuery, IReadOnlyList<AirportDto>>
{
    public async Task<IReadOnlyList<AirportDto>> Handle(SearchAirportsQuery r, CancellationToken ct) =>
        (await uow.Airports.SearchAsync(r.Search, r.Country, r.Type, Math.Max(r.PageNumber, 1), Math.Clamp(r.PageSize, 1, 100), ct))
            .Select(AirportDto.From).ToList();
}
