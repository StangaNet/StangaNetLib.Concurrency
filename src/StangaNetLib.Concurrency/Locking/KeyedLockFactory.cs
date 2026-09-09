namespace StangaNetLib.Concurrency.Locking;

internal sealed class KeyedLockFactory : IKeyedLockFactory
{
    public IKeyedLock Create() => new KeyedLock();
}
