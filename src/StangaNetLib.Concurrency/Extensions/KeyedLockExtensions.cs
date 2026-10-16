using StangaNetLib.Concurrency.Locking;

namespace StangaNetLib.Concurrency.Extensions;

/// <summary>
/// Convenience extensions for <see cref="IKeyedLock"/> that acquire the lock, execute an action,
/// and release the lock in a single call — eliminating <c>await using</c> boilerplate.
/// </summary>
public static class KeyedLockExtensions
{
    /// <summary>
    /// Acquires the lock for <paramref name="key"/>, executes <paramref name="action"/>,
    /// then releases the lock. Returns the value produced by <paramref name="action"/>.
    /// </summary>
    /// <typeparam name="T">The return type of the action.</typeparam>
    /// <param name="keyedLock">The keyed lock to use.</param>
    /// <param name="key">The resource key to lock on.</param>
    /// <param name="action">The async operation to execute while holding the lock.</param>
    /// <param name="cancellationToken">Token to cancel the wait for the lock.</param>
    /// <returns>The value returned by <paramref name="action"/>.</returns>
    public static async Task<T> ExecuteAsync<T>(
        this IKeyedLock keyedLock,
        string key,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await using var handle = await keyedLock.AcquireAsync(key, cancellationToken).ConfigureAwait(false);
        return await action(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Acquires the lock for <paramref name="key"/>, executes <paramref name="action"/>,
    /// then releases the lock.
    /// </summary>
    /// <param name="keyedLock">The keyed lock to use.</param>
    /// <param name="key">The resource key to lock on.</param>
    /// <param name="action">The async operation to execute while holding the lock.</param>
    /// <param name="cancellationToken">Token to cancel the wait for the lock.</param>
    public static async Task ExecuteAsync(
        this IKeyedLock keyedLock,
        string key,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        await using var handle = await keyedLock.AcquireAsync(key, cancellationToken).ConfigureAwait(false);
        await action(cancellationToken).ConfigureAwait(false);
    }
}
