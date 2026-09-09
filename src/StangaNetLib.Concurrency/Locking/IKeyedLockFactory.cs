namespace StangaNetLib.Concurrency.Locking;

/// <summary>
/// Creates independent <see cref="IKeyedLock"/> instances with their own per-key semaphore pools.
/// Use when multiple consumers need isolated lock namespaces — for example, a cache service that should
/// not share its lock pool with application-level business logic.
/// </summary>
public interface IKeyedLockFactory
{
    /// <summary>
    /// Creates a new <see cref="IKeyedLock"/> instance with an empty, independent semaphore pool.
    /// Each call returns a distinct instance; keys acquired on one instance have no effect on another.
    /// </summary>
    /// <returns>A new <see cref="IKeyedLock"/> with its own isolated semaphore pool.</returns>
    IKeyedLock Create();
}
