using MediatR;
using Microsoft.Extensions.Options;
using SkyWatch.Features.ReferenceData.Commands;
using SkyWatch.Features.ReferenceData.Dtos;
using SkyWatch.Features.ReferenceData.Queries;
using SkyWatch.Features.ReferenceData.Sources;

namespace SkyWatch.Services;

// Daily refresh of airports, airlines and countries. Not a cron job: it is a due-based scheduler. On startup
// it syncs whatever is missing or stale, then sleeps until the next source is due (every Interval, 24h by
// default). Sync state lives in the database, so restarting the API does not trigger a re-download.
public class ReferenceDataSyncService(
    IServiceScopeFactory scopes, IOptions<ReferenceDataOptions> options, ILogger<ReferenceDataSyncService> logger)
    : BackgroundService
{
    private readonly ReferenceDataOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Reference data sync is disabled");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan delay;
            try
            {
                using var scope = scopes.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                var states = await sender.Send(new GetSyncStatusQuery(), stoppingToken);
                var due = ReferenceSources.All.Where(s => DueAt(s, states) <= DateTime.UtcNow).ToList();
                if (due.Count > 0)
                {
                    logger.LogInformation("Reference data due for sync: {Sources}", string.Join(", ", due));
                    await sender.Send(new SyncReferenceDataCommand(due), stoppingToken);
                    states = await sender.Send(new GetSyncStatusQuery(), stoppingToken);
                }

                var next = ReferenceSources.All.Min(s => DueAt(s, states));
                delay = next - DateTime.UtcNow;
                if (delay < TimeSpan.FromMinutes(1)) delay = TimeSpan.FromMinutes(1);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reference data scheduler error; retrying in {Delay}", _options.RetryInterval);
                delay = _options.RetryInterval;
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    // Never synced -> due now. Last run failed -> retry after RetryInterval. Otherwise -> LastSyncedAt + Interval.
    private DateTime DueAt(string source, IReadOnlyList<SyncStateReturnDto> states)
    {
        var state = states.FirstOrDefault(s => s.Source == source);
        if (state?.LastAttemptAt is null) return DateTime.MinValue;
        return state.Status == "ok" && state.LastSyncedAt is { } synced
            ? synced + _options.Interval
            : state.LastAttemptAt.Value + _options.RetryInterval;
    }
}
