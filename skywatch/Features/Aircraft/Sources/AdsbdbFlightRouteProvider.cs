using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SkyWatch.Features.Aircraft.Models;

namespace SkyWatch.Features.Aircraft.Sources;

// Looks a callsign up on adsbdb.com. The API rate-limits (512 requests, then a 60s block) and asks for
// short-term caching only, so routes are cached for minutes, unknown callsigns and failures for less.
public class AdsbdbFlightRouteProvider(
    HttpClient http, IMemoryCache cache, IOptions<AircraftOptions> options, ILogger<AdsbdbFlightRouteProvider> logger)
    : IFlightRouteProvider
{
    private static readonly TimeSpan FailureCacheTtl = TimeSpan.FromSeconds(30);

    private readonly AircraftOptions _options = options.Value;

    // Wrapper so that "no route" can be cached too (IMemoryCache cannot tell a cached null from a miss).
    private sealed record CachedRoute(FlightRouteInfo? Route);

    public async Task<FlightRouteInfo?> GetRouteAsync(string callsign, CancellationToken ct)
    {
        callsign = callsign.Trim().ToUpperInvariant();
        var key = $"route:{callsign}";
        if (cache.TryGetValue(key, out CachedRoute? cached) && cached is not null) return cached.Route;

        try
        {
            var route = await FetchAsync(callsign, ct);
            cache.Set(key, new CachedRoute(route), route is null ? _options.UnknownRouteCacheTtl : _options.RouteCacheTtl);
            return route;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // The route is a nice-to-have: show the aircraft without it rather than failing the request.
            logger.LogWarning(ex, "Route lookup failed for {Callsign}", callsign);
            cache.Set(key, new CachedRoute(null), FailureCacheTtl);
            return null;
        }
    }

    private async Task<FlightRouteInfo?> FetchAsync(string callsign, CancellationToken ct)
    {
        using var response = await http.GetAsync($"v0/callsign/{Uri.EscapeDataString(callsign)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        await using var body = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(body, cancellationToken: ct);

        // An unknown callsign can come back as {"response":"unknown callsign"} instead of an object.
        if (!document.RootElement.TryGetProperty("response", out var wrapper)
            || wrapper.ValueKind != JsonValueKind.Object
            || !wrapper.TryGetProperty("flightroute", out var flight)
            || flight.ValueKind != JsonValueKind.Object)
            return null;

        var airline = flight.TryGetProperty("airline", out var airlineElement) && airlineElement.ValueKind == JsonValueKind.Object
            ? Text(airlineElement, "name") : null;
        var origin = Airport(flight, "origin");
        var destination = Airport(flight, "destination");
        return origin is null && destination is null ? null : new FlightRouteInfo(callsign, airline, origin, destination);
    }

    private static RouteAirport? Airport(JsonElement flight, string name)
    {
        if (!flight.TryGetProperty(name, out var airport) || airport.ValueKind != JsonValueKind.Object) return null;
        if (!Number(airport, "latitude", out var latitude) || !Number(airport, "longitude", out var longitude)) return null;
        return new RouteAirport(Text(airport, "iata_code"), Text(airport, "icao_code"), Text(airport, "name") ?? "",
            Text(airport, "municipality"), Text(airport, "country_iso_name"), latitude, longitude);
    }

    private static string? Text(JsonElement item, string name) =>
        item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString() : null;

    private static bool Number(JsonElement item, string name, out double number)
    {
        number = 0;
        return item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out number);
    }
}
