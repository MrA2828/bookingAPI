using MediatR;
using SkyWatch.Features.Airports.Dtos;
using SkyWatch.Repositories;

namespace SkyWatch.Features.Airports.Queries;

public record SearchAirlinesQuery(string? Q, int Limit, int Offset) : IRequest<IReadOnlyList<AirlineDto>>;

public class SearchAirlinesHandler(IUnitOfWork uow) : IRequestHandler<SearchAirlinesQuery, IReadOnlyList<AirlineDto>>
{
    public async Task<IReadOnlyList<AirlineDto>> Handle(SearchAirlinesQuery r, CancellationToken ct) =>
        (await uow.Airlines.SearchAsync(r.Q, Math.Clamp(r.Limit, 1, 100), Math.Max(r.Offset, 0), ct))
            .Select(a => new AirlineDto(a.Id, a.Name, a.IataCode, a.IcaoCode, a.Callsign, a.Country)).ToList();
}
