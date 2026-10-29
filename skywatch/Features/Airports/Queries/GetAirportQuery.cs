using MediatR;
using SkyWatch.Features.Airports.Dtos;
using SkyWatch.Repositories;

namespace SkyWatch.Features.Airports.Queries;

public record GetAirportQuery(string Code) : IRequest<AirportDto?>;

public class GetAirportHandler(IUnitOfWork uow) : IRequestHandler<GetAirportQuery, AirportDto?>
{
    public async Task<AirportDto?> Handle(GetAirportQuery r, CancellationToken ct) =>
        await uow.Airports.FindByCodeAsync(r.Code, ct) is { } airport ? AirportDto.From(airport) : null;
}
