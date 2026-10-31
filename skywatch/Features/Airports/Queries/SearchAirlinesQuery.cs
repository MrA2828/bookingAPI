using MediatR;
using SkyWatch.Features.Airports.Dtos;
using SkyWatch.Repositories;

namespace SkyWatch.Features.Airports.Queries;

public record SearchAirlinesQuery(SearchAirlinesRequestDto Request) : IRequest<IReadOnlyList<AirlineReturnDto>>;

public class SearchAirlinesHandler(IUnitOfWork uow) : IRequestHandler<SearchAirlinesQuery, IReadOnlyList<AirlineReturnDto>>
{
    public async Task<IReadOnlyList<AirlineReturnDto>> Handle(SearchAirlinesQuery r, CancellationToken ct) =>
        (await uow.Airlines.SearchAsync(r.Request.Search, Math.Max(r.Request.PageNumber, 1), Math.Clamp(r.Request.PageSize, 1, 100), ct))
            .Select(a => new AirlineReturnDto(a.Id, a.Name, a.IataCode, a.IcaoCode, a.Callsign, a.Country)).ToList();
}
