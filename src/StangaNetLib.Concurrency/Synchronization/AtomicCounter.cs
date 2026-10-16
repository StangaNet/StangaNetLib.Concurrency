namespace StangaNetLib.Concurrency.Synchronization;

internal sealed class AtomicCounter : IAtomicCounter
{
    private long _value;

    internal AtomicCounter(long initialValue = 0) => _value = initialValue;

    public long Value => Interlocked.Read(ref _value);

    public long Increment() => Interlocked.Increment(ref _value);

    public long Decrement() => Interlocked.Decrement(ref _value);

    public long Add(long delta) => Interlocked.Add(ref _value, delta);

    public long Reset(long value = 0) => Interlocked.Exchange(ref _value, value);

    public bool TryUpdate(long expectedValue, long newValue)
        => Interlocked.CompareExchange(ref _value, newValue, expectedValue) == expectedValue;
}
