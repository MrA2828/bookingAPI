using MediatR;
using SkyWatch.Features.ReferenceData.Dtos;
using SkyWatch.Repositories;

namespace SkyWatch.Features.ReferenceData.Queries;

public record GetCountriesQuery : IRequest<IReadOnlyList<CountryDto>>;

public class GetCountriesHandler(IUnitOfWork uow) : IRequestHandler<GetCountriesQuery, IReadOnlyList<CountryDto>>
{
    public async Task<IReadOnlyList<CountryDto>> Handle(GetCountriesQuery request, CancellationToken ct) =>
        (await uow.Countries.ListAsync(ct)).Select(c => new CountryDto(c.Code, c.Name, c.Continent)).ToList();
}
