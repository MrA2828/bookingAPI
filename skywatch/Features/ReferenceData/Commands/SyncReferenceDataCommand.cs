using MediatR;
using SkyWatch.Entities;
using SkyWatch.Features.ReferenceData.Sources;
using SkyWatch.Repositories;

namespace SkyWatch.Features.ReferenceData.Commands;

public record SyncReferenceDataCommand(IReadOnlyCollection<string> Sources) : IRequest<IReadOnlyList<SyncSourceResult>>;

public record SyncSourceResult(string Source, bool Success, int Rows, int Added, int Updated, int Deactivated, string? Error);

public class SyncReferenceDataHandler(IUnitOfWork uow, IReferenceDataSource source, ILogger<SyncReferenceDataHandler> logger)
    : IRequestHandler<SyncReferenceDataCommand, IReadOnlyList<SyncSourceResult>>
{
    // Airport types worth keeping; heliports, seaplane bases, balloonports and closed fields are skipped,
    // so an airport that closes drops out of the source and is deactivated here.
    private static readonly HashSet<string> AirportTypes = ["large_airport", "medium_airport", "small_airport"];

    private record Counts(int Rows, int Added, int Updated, int Deactivated);

    public async Task<IReadOnlyList<SyncSourceResult>> Handle(SyncReferenceDataCommand request, CancellationToken ct)
    {
        var results = new List<SyncSourceResult>();
        // Countries first, then the rest in the order requested.
        foreach (var name in ReferenceSources.All.Where(request.Sources.Contains))
        {
            results.Add(await RunAsync(name, ct));
        }
        return results;
    }

    // Each source is applied and its sync state saved in one SaveChanges, so a source is either fully
    // updated or untouched. A failure in one source does not stop the others.
    private async Task<SyncSourceResult> RunAsync(string name, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        try
        {
            var counts = name switch
            {
                ReferenceSources.Countries => await SyncCountriesAsync(ct),
                ReferenceSources.Airports => await SyncAirportsAsync(now, ct),
                ReferenceSources.Airlines => await SyncAirlinesAsync(now, ct),
                _ => throw new ArgumentException($"Unknown reference source '{name}'"),
            };

            var state = await GetOrAddStateAsync(name, ct);
            state.Status = "ok";
            state.LastAttemptAt = state.LastSyncedAt = now;
            (state.Rows, state.Added, state.Updated, state.Deactivated, state.Error) =
                (counts.Rows, counts.Added, counts.Updated, counts.Deactivated, null);
            await uow.SaveChangesAsync(ct);
            uow.DiscardChanges();   // release the tracked rows before the next source

            logger.LogInformation("Reference sync '{Source}': {Rows} rows, +{Added} ~{Updated} -{Deactivated}",
                name, counts.Rows, counts.Added, counts.Updated, counts.Deactivated);
            return new SyncSourceResult(name, true, counts.Rows, counts.Added, counts.Updated, counts.Deactivated, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Reference sync '{Source}' failed", name);
            uow.DiscardChanges();   // throw away any half-applied changes, keep last good data

            var state = await GetOrAddStateAsync(name, ct);
            state.Status = "failed";
            state.LastAttemptAt = now;
            state.Error = ex.Message;
            await uow.SaveChangesAsync(ct);
            uow.DiscardChanges();
            return new SyncSourceResult(name, false, 0, 0, 0, 0, ex.Message);
        }
    }

    private async Task<SyncState> GetOrAddStateAsync(string name, CancellationToken ct)
    {
        var state = await uow.SyncStates.FindAsync(name, ct);
        if (state is null)
        {
            state = new SyncState { Source = name };
            uow.SyncStates.Add(state);
        }
        return state;
    }

    private async Task<Counts> SyncCountriesAsync(CancellationToken ct)
    {
        var records = await source.FetchCountriesAsync(ct);
        var existing = await uow.Countries.GetAllByCodeAsync(ct);
        EnsureSane(records.Count, existing.Count, ReferenceSources.Countries);

        int added = 0, updated = 0;
        foreach (var r in records.DistinctBy(r => r.Code))
        {
            if (existing.TryGetValue(r.Code, out var country))
            {
                if (country.Name == r.Name && country.Continent == r.Continent) continue;
                (country.Name, country.Continent) = (r.Name, r.Continent);
                updated++;
            }
            else
            {
                uow.Countries.Add(new Country { Code = r.Code, Name = r.Name, Continent = r.Continent });
                added++;
            }
        }
        return new Counts(records.Count, added, updated, 0);   // countries are never deactivated
    }

    private async Task<Counts> SyncAirportsAsync(DateTime now, CancellationToken ct)
    {
        var records = (await source.FetchAirportsAsync(ct)).Where(r => AirportTypes.Contains(r.Type)).ToList();
        var existing = await uow.Airports.GetAllByIdAsync(ct);
        EnsureSane(records.Count, existing.Values.Count(a => a.IsActive), ReferenceSources.Airports);

        return Merge(records, existing, r => r.Id, Airport.From,
            (e, r) => { if (e.ToRecord() == r) return false; e.Apply(r); return true; },
            uow.Airports.Add, now);
    }

    private async Task<Counts> SyncAirlinesAsync(DateTime now, CancellationToken ct)
    {
        var records = await source.FetchAirlinesAsync(ct);
        var existing = await uow.Airlines.GetAllByIdAsync(ct);
        EnsureSane(records.Count, existing.Values.Count(a => a.IsActive), ReferenceSources.Airlines);

        return Merge(records, existing, r => r.Id, Airline.From,
            (e, r) => { if (e.ToRecord() == r) return false; e.Apply(r); return true; },
            uow.Airlines.Add, now);
    }

    // Adds new rows, updates changed ones, re-activates returning ones and deactivates rows
    // that are no longer in the source.
    private static Counts Merge<TRecord, TEntity>(
        IReadOnlyList<TRecord> records, Dictionary<int, TEntity> existing, Func<TRecord, int> keyOf,
        Func<TRecord, TEntity> create, Func<TEntity, TRecord, bool> apply, Action<TEntity> add, DateTime now)
        where TEntity : class, ISyncedEntity
    {
        var seen = new HashSet<int>();
        int added = 0, updated = 0, deactivated = 0;

        foreach (var record in records)
        {
            var id = keyOf(record);
            if (!seen.Add(id)) continue;   // duplicate id in the source

            if (existing.TryGetValue(id, out var entity))
            {
                var changed = apply(entity, record);
                if (!entity.IsActive)
                {
                    entity.IsActive = true;
                    changed = true;
                }
                if (changed)
                {
                    entity.UpdatedAt = now;
                    updated++;
                }
            }
            else
            {
                var created = create(record);
                created.UpdatedAt = now;
                add(created);
                added++;
            }
        }

        foreach (var entity in existing.Values.Where(e => e.IsActive && !seen.Contains(e.Id)))
        {
            entity.IsActive = false;
            entity.UpdatedAt = now;
            deactivated++;
        }
        return new Counts(seen.Count, added, updated, deactivated);
    }

    // Guards against a truncated or empty download wiping out good data.
    private static void EnsureSane(int fetched, int existing, string source)
    {
        if (fetched == 0 || (existing >= 100 && fetched < existing * 0.5))
            throw new InvalidOperationException(
                $"Source '{source}' returned {fetched} rows but {existing} are stored; refusing to apply it.");
    }
}
