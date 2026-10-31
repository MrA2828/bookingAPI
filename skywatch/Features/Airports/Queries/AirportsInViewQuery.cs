using MediatR;
using SkyWatch.Features.Airports.Dtos;
using SkyWatch.Repositories;

namespace SkyWatch.Features.Airports.Queries;

public record AirportsInViewQuery(AirportsInViewRequestDto Request) : IRequest<IReadOnlyList<AirportReturnDto>>;

public class AirportsInViewHandler(IUnitOfWork uow) : IRequestHandler<AirportsInViewQuery, IReadOnlyList<AirportReturnDto>>
{
    public async Task<IReadOnlyList<AirportReturnDto>> Handle(AirportsInViewQuery r, CancellationToken ct)
    {
        var request = r.Request;
        var airports = await uow.Airports.InViewAsync(
            request.South, request.North, request.West, request.East, request.ScheduledOnly, request.Types, request.Limit, ct);
        return airports.Select(AirportReturnDto.From).ToList();
    }
}
