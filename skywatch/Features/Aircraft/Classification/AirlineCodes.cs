using Microsoft.Extensions.Caching.Memory;
using SkyWatch.Repositories;

namespace SkyWatch.Features.Aircraft.Classification;

public class AirlineCodes(IUnitOfWork uow, IMemoryCache cache) : IAirlineCodes
{
    private const string CacheKey = "airline-icao-codes";

    public async Task<IReadOnlySet<string>> GetIcaoCodesAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlySet<string>? cached) && cached is not null) return cached;

        var codes = (await uow.Airlines.GetActiveIcaoCodesAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // Short enough to pick up the daily reference sync, long enough to avoid a query per request.
        cache.Set(CacheKey, (IReadOnlySet<string>)codes, TimeSpan.FromHours(1));
        return codes;
    }
}
