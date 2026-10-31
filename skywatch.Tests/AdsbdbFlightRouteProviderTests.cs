using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkyWatch.Features.Aircraft.Sources;

namespace SkyWatch.Tests;

public class AdsbdbFlightRouteProviderTests
{
    private const string Body = """
        {"response":{"flightroute":{"airline":{"name":"British Airways"},
        "origin":{"iata_code":"LHR","icao_code":"EGLL","name":"Heathrow","latitude":51.47,"longitude":-0.45},
        "destination":{"iata_code":"JFK","icao_code":"KJFK","name":"Kennedy","latitude":40.64,"longitude":-73.78}}}}
        """;

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Calls;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            await Task.Delay(100, ct);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Body, Encoding.UTF8, "application/json") };
        }
    }

    [Fact]
    public async Task Concurrent_lookups_of_one_callsign_share_a_single_upstream_call()
    {
        var handler = new CountingHandler();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var locks = new KeyedLock();

        // One provider per request, like the typed-client registration, sharing the cache and the lock.
        var routes = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ =>
            new AdsbdbFlightRouteProvider(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, cache, locks,
                Options.Create(new AircraftOptions()), NullLogger<AdsbdbFlightRouteProvider>.Instance)
                .GetRouteAsync("BAW123", default)));

        Assert.Equal(1, handler.Calls);
        Assert.All(routes, r => Assert.Equal("LHR", r!.Origin!.IataCode));
    }
}
