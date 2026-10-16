using StangaNetLib.Core.Common;

namespace StangaNetLib.Concurrency.Locking;

/// <summary>
/// Provides per-key async mutual exclusion. Useful for serializing operations that share the same
/// logical resource (e.g., the same entity ID) without blocking unrelated resources.
/// </summary>
public interface IKeyedLock
{
    /// <summary>
    /// Waits indefinitely until the lock for <paramref name="key"/> is acquired,
    /// then returns a handle whose disposal releases the lock.
    /// </summary>
    /// <param name="key">The resource key to lock on.</param>
    /// <param name="cancellationToken">Token to cancel the wait.</param>
    /// <returns>An <see cref="IAsyncDisposable"/> that releases the lock when disposed.</returns>
    Task<IAsyncDisposable> AcquireAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire the lock for <paramref name="key"/> within <paramref name="timeout"/>.
    /// Returns a failure <see cref="Result{T}"/> with <c>ConcurrencyErrors.LockTimeout</c> when the
    /// timeout elapses without acquiring the lock.
    /// </summary>
    /// <param name="key">The resource key to lock on.</param>
    /// <param name="timeout">Maximum time to wait for the lock.</param>
    /// <param name="cancellationToken">Token to cancel the wait.</param>
    /// <returns>
    /// A successful <see cref="Result{T}"/> wrapping an <see cref="IAsyncDisposable"/> lock handle,
    /// or a failure with <c>ConcurrencyErrors.LockTimeout</c> if the timeout elapsed.
    /// </returns>
    Task<Result<IAsyncDisposable>> TryAcquireAsync(string key, TimeSpan timeout, CancellationToken cancellationToken = default);
}
