using MediatR;
using SkyWatch.Features.Airports.Dtos;
using SkyWatch.Repositories;

namespace SkyWatch.Features.Airports.Queries;

public record GetAirportQuery(string Code) : IRequest<AirportReturnDto?>;

public class GetAirportHandler(IUnitOfWork uow) : IRequestHandler<GetAirportQuery, AirportReturnDto?>
{
    public async Task<AirportReturnDto?> Handle(GetAirportQuery r, CancellationToken ct) =>
        await uow.Airports.FindByCodeAsync(r.Code, ct) is { } airport ? AirportReturnDto.From(airport) : null;
}
