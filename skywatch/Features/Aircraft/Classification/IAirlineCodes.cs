namespace SkyWatch.Features.Aircraft.Classification;

public interface IAirlineCodes
{
    // ICAO designators (BAW, UAL, ...) of every active airline, cached in memory.
    Task<IReadOnlySet<string>> GetIcaoCodesAsync(CancellationToken ct);
}
