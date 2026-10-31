using MediatR;
using SkyWatch.Features.Airports.Dtos;
using SkyWatch.Repositories;

namespace SkyWatch.Features.Airports.Queries;

public record SearchAirportsQuery(SearchAirportsRequestDto Request) : IRequest<IReadOnlyList<AirportReturnDto>>;

public class SearchAirportsHandler(IUnitOfWork uow) : IRequestHandler<SearchAirportsQuery, IReadOnlyList<AirportReturnDto>>
{
    public async Task<IReadOnlyList<AirportReturnDto>> Handle(SearchAirportsQuery r, CancellationToken ct) =>
        (await uow.Airports.SearchAsync(r.Request.Search, r.Request.Country, r.Request.Type, Math.Max(r.Request.PageNumber, 1), Math.Clamp(r.Request.PageSize, 1, 100), ct))
            .Select(AirportReturnDto.From).ToList();
}
