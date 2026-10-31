using MediatR;
using SkyWatch.Features.ReferenceData.Dtos;
using SkyWatch.Repositories;

namespace SkyWatch.Features.ReferenceData.Queries;

public record GetCountriesQuery : IRequest<IReadOnlyList<CountryReturnDto>>;

public class GetCountriesHandler(IUnitOfWork uow) : IRequestHandler<GetCountriesQuery, IReadOnlyList<CountryReturnDto>>
{
    public async Task<IReadOnlyList<CountryReturnDto>> Handle(GetCountriesQuery request, CancellationToken ct) =>
        (await uow.Countries.ListAsync(ct)).Select(c => new CountryReturnDto(c.Code, c.Name, c.Continent)).ToList();
}
