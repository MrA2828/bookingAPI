namespace SkyWatch.Features.Aircraft.Sources;

// One lock per key, created on demand and dropped when nobody holds or awaits it. Registered as a singleton so
// that concurrent requests (each with its own scoped AircraftFeed) queue up on the same lock.
public class KeyedLock
{
    private readonly Dictionary<string, Entry> _entries = new();

    private sealed class Entry
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int Users;
    }

    public async Task<IDisposable> AcquireAsync(string key, CancellationToken ct)
    {
        Entry entry;
        lock (_entries)
        {
            if (!_entries.TryGetValue(key, out entry!))
                _entries[key] = entry = new Entry();
            entry.Users++;
        }

        try
        {
            await entry.Semaphore.WaitAsync(ct);
        }
        catch
        {
            Release(key, entry, held: false);
            throw;
        }
        return new Releaser(() => Release(key, entry, held: true));
    }

    private void Release(string key, Entry entry, bool held)
    {
        if (held) entry.Semaphore.Release();
        lock (_entries)
        {
            if (--entry.Users == 0) _entries.Remove(key);
        }
    }

    private sealed class Releaser(Action release) : IDisposable
    {
        private Action? _release = release;
        public void Dispose() => Interlocked.Exchange(ref _release, null)?.Invoke();
    }
}
