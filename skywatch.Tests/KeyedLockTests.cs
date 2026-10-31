using SkyWatch.Features.Aircraft.Sources;

namespace SkyWatch.Tests;

public class KeyedLockTests
{
    [Fact]
    public async Task Same_key_runs_one_holder_at_a_time()
    {
        var locks = new KeyedLock();
        var inside = 0;
        var maxInside = 0;

        await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
        {
            using var held = await locks.AcquireAsync("k", default);
            maxInside = Math.Max(maxInside, Interlocked.Increment(ref inside));
            await Task.Delay(5);
            Interlocked.Decrement(ref inside);
        }));

        Assert.Equal(1, maxInside);
    }

    [Fact]
    public async Task Different_keys_do_not_block_each_other()
    {
        var locks = new KeyedLock();
        using var first = await locks.AcquireAsync("a", default);

        using var second = await locks.AcquireAsync("b", new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);
    }

    [Fact]
    public async Task A_cancelled_waiter_does_not_leave_the_key_locked()
    {
        var locks = new KeyedLock();
        var first = await locks.AcquireAsync("k", default);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => locks.AcquireAsync("k", cts.Token));

        first.Dispose();
        using var again = await locks.AcquireAsync("k", new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);
    }
}
