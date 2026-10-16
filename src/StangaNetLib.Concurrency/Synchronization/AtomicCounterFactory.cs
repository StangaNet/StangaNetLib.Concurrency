using System.Collections.Concurrent;

namespace StangaNetLib.Concurrency.Synchronization;

internal sealed class AtomicCounterFactory : IAtomicCounterFactory
{
    private readonly ConcurrentDictionary<string, IAtomicCounter> _named = new(StringComparer.Ordinal);

    public IAtomicCounter Create(long initialValue = 0) => new AtomicCounter(initialValue);

    public IAtomicCounter GetOrCreate(string name, long initialValue = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _named.GetOrAdd(name, _ => new AtomicCounter(initialValue));
    }
}
