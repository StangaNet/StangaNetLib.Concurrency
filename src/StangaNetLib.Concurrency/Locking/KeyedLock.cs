using System.Collections.Concurrent;
using StangaNetLib.Core.Common;
using StangaNetLib.Concurrency.Core;

namespace StangaNetLib.Concurrency.Locking;

/// <summary>
/// Per-key mutual exclusion backed by a <see cref="ConcurrentDictionary{TKey,TValue}"/> of
/// <see cref="SemaphoreSlim"/> entries. Reference counting ensures entries are removed from the
/// dictionary only when no waiter holds or is waiting on a given key.
/// </summary>
internal sealed class KeyedLock : IKeyedLock
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public async Task<IAsyncDisposable> AcquireAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var entry = IncrementRef(key);
        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            DecrementRef(key, entry);
            throw;
        }
        return new LockHandle(this, key, entry);
    }

    public async Task<Result<IAsyncDisposable>> TryAcquireAsync(string key, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var entry = IncrementRef(key);
        bool acquired;
        try
        {
            acquired = await entry.Semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            DecrementRef(key, entry);
            throw;
        }

        if (!acquired)
        {
            DecrementRef(key, entry);
            return Result<IAsyncDisposable>.Failure(ConcurrencyErrors.LockTimeout);
        }
        return Result<IAsyncDisposable>.Success(new LockHandle(this, key, entry));
    }

    private Entry IncrementRef(string key)
    {
        while (true)
        {
            var entry = _entries.GetOrAdd(key, _ => new Entry());
            Interlocked.Increment(ref entry.RefCount);
            // Re-read from the dictionary after incrementing the ref count. If the entry we
            // incremented is still the live one, we own a valid reference. If it was concurrently
            // removed and replaced (because the last waiter decremented ref to 0), we undo the
            // increment and retry with the new entry.
            if (_entries.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
                return entry;
            Interlocked.Decrement(ref entry.RefCount);
        }
    }

    internal void ReleaseLock(string key, Entry entry)
    {
        entry.Semaphore.Release();
        DecrementRef(key, entry);
    }

    private void DecrementRef(string key, Entry entry)
    {
        if (Interlocked.Decrement(ref entry.RefCount) == 0)
            _entries.TryRemove(new KeyValuePair<string, Entry>(key, entry));
    }

    internal sealed class Entry
    {
        public readonly SemaphoreSlim Semaphore = new(1, 1);
        public int RefCount;
    }

    private sealed class LockHandle : IAsyncDisposable
    {
        private readonly KeyedLock _owner;
        private readonly string _key;
        private readonly Entry _entry;
        private int _disposed;

        internal LockHandle(KeyedLock owner, string key, Entry entry)
        {
            _owner = owner;
            _key = key;
            _entry = entry;
        }

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                _owner.ReleaseLock(_key, _entry);
            return ValueTask.CompletedTask;
        }
    }
}
